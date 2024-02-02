using Carter;
using FluentValidation;
using Mapster;
using MediatR;
using Web.Api.Contracts.Projects.Update;
using Web.Api.Database;
using Web.Api.Entities;
using Web.Api.Extensions.CurrentUserService;
using Web.Api.Shared;

namespace Web.Api.Features.Projects;

public class UpdateProject
{
    public class Command : IRequest<Result> 
    {
        public int ProjectId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ProjectId).NotEmpty();
            RuleFor(x => x.Name).MaximumLength(Entity.NameMaxLength);
            RuleFor(x => x.Description).MaximumLength(Entity.DescriptionMaxLength);
        }
    }
    
    internal sealed class Handler : IRequestHandler<UpdateProject.Command, Result>
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
                    return Result.Failure(new Error("UpdateProject.Validation", validationResult.ToString()));
                
                if (!await _currentUserService.CanModerateProject(request.ProjectId, cancellationToken))
                    return Result.Failure(new Error("UpdateProject.NoAccess", "Access denied"));
                
                var success = await UpdateProject(request.ProjectId, request.Name, request.Description, cancellationToken);
                
                if (!success)
                    return Result.Failure(new Error("UpdateProject.NoAccess", "Access denied"));
                
                await _dbContext.SaveChangesAsync(cancellationToken);
                return Result.Success();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return Result.Failure(new Error("UpdateProject.Exception", e.ToString()));
            }
        }

        public async Task<bool> UpdateProject(int projectId, string name, string description, CancellationToken cancellationToken = default)
        {
            var project = _dbContext.Projects.FirstOrDefault(x => x.Id == projectId);

            if (project != null)
            {
                project.Name = name;
                project.Description = description;
                _dbContext.Projects.Update(project);
                return true;
            }

            return false;
        }
    }
}

public class UpdateProjectEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("api/projects/{id}", async (UpdateProjectRequest request, ISender sender) =>
        {
            var command = request.Adapt<UpdateProject.Command>();

            var result = await sender.Send(command);
            if (result.IsFailure)
                return Results.BadRequest(result.Error);

            return Results.Ok(result);
        }).RequireAuthorization().WithTags("Projects");
    }
}