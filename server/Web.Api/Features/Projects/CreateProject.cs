using Carter;
using FluentValidation;
using Mapster;
using MediatR;
using Web.Api.Contracts.Projects.Create;
using Web.Api.Database;
using Web.Api.Entities;
using Web.Api.Extensions.CurrentUserService;
using Web.Api.Shared;

namespace Web.Api.Features.Projects;

public class CreateProject
{
    public class Command : IRequest<Result<int>>
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Name).MaximumLength(Entity.NameMaxLength);
            RuleFor(x => x.Description).MaximumLength(Entity.DescriptionMaxLength);
        }
    }
    
    internal sealed class Handler : IRequestHandler<CreateProject.Command, Result<int>>
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
                    return Result.Failure<int>(new Error("CreateProject.Validation", validationResult.ToString()));
                var user = _currentUserService.UserId;
                var project = new Project()
                {
                    Name = request.Name,
                    Description = request.Description,
                    UserProjects = new List<UserProject>() {new UserProject()
                    {
                        UserId = user,
                        Rank = UserProjectRankEnum.Owner
                    }}
                };
                
                await _dbContext.Projects.AddAsync(project, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
                return project.Id;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return Result.Failure<int>(new Error("CreateProject.Exception", e.ToString()));
            }
        }
    }
}

public class CreateProjectEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("api/projects", async (CreateProjectRequest request, ISender sender) =>
        {
            var command = request.Adapt<CreateProject.Command>();

            var result = await sender.Send(command);
            if (result.IsFailure)
                return Results.BadRequest(result.Error);

            return Results.Ok(result.Value);
        }).RequireAuthorization().WithTags("Projects");
    }
}