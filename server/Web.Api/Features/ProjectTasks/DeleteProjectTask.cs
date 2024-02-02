using Carter;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Web.Api.Database;
using Web.Api.Extensions.CurrentUserService;
using Web.Api.Shared;

namespace Web.Api.Features.ProjectTasks;

public class DeleteProjectTask
{
    public class Command : IRequest<Result>
    {
        public int ProjectTaskId { get; set; }
    }
    
    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ProjectTaskId).NotEmpty();
        }
    }
    
    internal sealed class Handler : IRequestHandler<Command, Result>
    {
        private readonly ApplicationDBContext _dbContext;
        private readonly IValidator<Command> _validator;
        private readonly ICurrentUserService _currentUserService;

        public Handler(ApplicationDBContext dbContext, IValidator<Command> validator, ICurrentUserService currentUserService)
        {
            _dbContext = dbContext;
            _validator = validator;
            _currentUserService = currentUserService;
        }

        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            try
            {
                var validationResult = await _validator.ValidateAsync(request, cancellationToken);
                if (!validationResult.IsValid)
                    return Result.Failure<int>(new Error("DeleteProjectTask.Validation", validationResult.ToString()));

                var projectId = await _dbContext.ProjectTasks.Where(x => x.Id == request.ProjectTaskId)
                    .Select(x => x.ProjectId).FirstOrDefaultAsync(cancellationToken);
                
                if (!await _currentUserService.CanModerateProject(projectId, cancellationToken))
                    return Result.Failure<int>(new Error("DeleteProjectTask.NoAccess", "Access denied"));

                await DeleteProjectTask(request.ProjectTaskId, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
                return Result.Success();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return Result.Failure<int>(new Error("DeleteProjectTask", e.ToString()));
            }
        }

        public async Task DeleteProjectTask(int projectTaskId, CancellationToken cancellationToken = default)
        {
            var projectTask = _dbContext.ProjectTasks.FirstOrDefault(x => x.Id == projectTaskId);
            if (projectTask != null) 
                _dbContext.ProjectTasks.Remove(projectTask);
        }

    }
}

public class DeleteProjectTaskEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete("api/projecttasks/{id}", async (int projectTaskId, ISender sender) =>
        {
            var command = new DeleteProjectTask.Command() { ProjectTaskId = projectTaskId };

            var result = await sender.Send(command);
            if (result.IsFailure)
                return Results.BadRequest(result.Error);

            return Results.Ok(result);
        }).RequireAuthorization().WithTags("ProjectTasks");
    }
}