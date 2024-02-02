using Carter;
using FluentValidation;
using Mapster;
using MediatR;
using Web.Api.Contracts.ProjectTasks.Create;
using Web.Api.Database;
using Web.Api.Entities;
using Web.Api.Extensions.CurrentUserService;
using Web.Api.Shared;

namespace Web.Api.Features.ProjectTasks;

public class CreateProjectTask
{
    public class Command : IRequest<Result<int>>
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public TaskStatusEnum TaskStatus { get; set; }
        public int ProjectId { get; set; }
    }
    
    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Name).MaximumLength(Entity.NameMaxLength);
            RuleFor(x => x.Description).MaximumLength(Entity.DescriptionMaxLength);
        }
    }
    
    internal sealed class Handler : IRequestHandler<Command, Result<int>>
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

        public async Task<Result<int>> Handle(Command request, CancellationToken cancellationToken)
        {
            try
            {
                var validationResult = await _validator.ValidateAsync(request, cancellationToken);
                if (!validationResult.IsValid)
                    return Result.Failure<int>(new Error("CreateProjectTask.Validation", validationResult.ToString()));
                if (!await _currentUserService.IsProjectMember(request.ProjectId, cancellationToken))
                    return Result.Failure<int>(new Error("CreateProjectTask.NoAccess", "Access denied"));
                var user = _currentUserService.UserId;
                var projectTask = new ProjectTask()
                {
                    Name = request.Name,
                    Description = request.Description,
                    TaskStatus = request.TaskStatus,
                    ProjectId = request.ProjectId,
                };
                await _dbContext.ProjectTasks.AddAsync(projectTask, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
                return projectTask.Id;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return Result.Failure<int>(new Error("CreateProjectTask.Exception", e.ToString()));
            }
        }
    }
}

public class CreateProjectTaskEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("api/projecttasks", async (CreateProjectTaskRequest request, ISender sender) =>
        {
            var command = request.Adapt<CreateProjectTask.Command>();

            var result = await sender.Send(command);
            if (result.IsFailure)
                return Results.BadRequest(result.Error);

            return Results.Ok(result.Value);
        }).RequireAuthorization().WithTags("ProjectTasks");
    }
}