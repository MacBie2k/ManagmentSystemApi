using Carter;
using FluentValidation;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Web.Api.Contracts.ProjectTasks.Update;
using Web.Api.Database;
using Web.Api.Entities;
using Web.Api.Extensions.CurrentUserService;
using Web.Api.Shared;

namespace Web.Api.Features.ProjectTasks;

public class UpdateProjectTask
{
    public class Command : IRequest<Result> 
    {
        public int ProjectTaskId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
    
    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ProjectTaskId).NotEmpty();
            RuleFor(x => x.Name).MaximumLength(Entity.NameMaxLength);
            RuleFor(x => x.Description).MaximumLength(Entity.DescriptionMaxLength);
        }
    }
    
    internal sealed class Handler : IRequestHandler<UpdateProjectTask.Command, Result>
    {
        private readonly ApplicationDBContext _dbContext;
        private readonly ICurrentUserService _currentUserService;
        private readonly IValidator<Command> _validator;

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
                    return Result.Failure(new Error("UpdateProjectTask.Validation", validationResult.ToString()));

                var projectId = await _dbContext.ProjectTasks.Where(x => x.Id == request.ProjectTaskId)
                    .Select(x => x.ProjectId).FirstOrDefaultAsync(cancellationToken);
                
                if (!await _currentUserService.CanModerateProject(projectId, cancellationToken))
                    return Result.Failure(new Error("UpdateProjectTask.NoAccess", "Access denied"));
                
                var success = await UpdateProjectTask(request.ProjectTaskId, request.Name, request.Description, cancellationToken);
                
                if (!success)
                    return Result.Failure(new Error("UpdateProjectTask.NoAccess", "Access denied"));
                
                await _dbContext.SaveChangesAsync(cancellationToken);
                return Result.Success();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return Result.Failure(new Error("UpdateProjectTask.Excetpion", e.ToString()));
            }
        }
        public async Task<bool> UpdateProjectTask(int projectTaskId, string name, string description, CancellationToken cancellationToken = default)
        {
            var projectTask = _dbContext.ProjectTasks.FirstOrDefault(x => x.Id == projectTaskId);

            if (projectTask != null)
            {
                projectTask.Name = name;
                projectTask.Description = description;
                _dbContext.ProjectTasks.Update(projectTask);
                return true;
            }

            return false;
        }
    }
}

public class UpdateProjectTaskEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("api/projecttasks/{id}", async (UpdateProjectTaskRequest request, ISender sender) =>
        {
            var command = request.Adapt<UpdateProjectTask.Command>();

            var result = await sender.Send(command);
            if (result.IsFailure)
                return Results.BadRequest(result.Error);

            return Results.Ok(result);
        }).RequireAuthorization().WithTags("ProjectTasks");
    }
}