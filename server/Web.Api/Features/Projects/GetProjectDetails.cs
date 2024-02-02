using Carter;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Web.Api.Contracts.Projects.Details;
using Web.Api.Database;
using Web.Api.Dtos.Project;
using Web.Api.Dtos.ProjectTask;
using Web.Api.Dtos.UserProjects;
using Web.Api.Dtos.Users;
using Web.Api.Extensions.CurrentUserService;
using Web.Api.Shared;

namespace Web.Api.Features.Projects;

public class GetProjectDetails
{
    public class Query : IRequest<Result<GetProjectDetailsResponse>>
    {
        public int ProjectId { get; set; }
    }
    
    public class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ProjectId).NotEmpty();
        }
    }
    
    internal sealed class Handler : IRequestHandler<Query, Result<GetProjectDetailsResponse>>
    {
        
        private readonly ApplicationDBContext _dbContext;
        private readonly IValidator<Query> _validator;
        private readonly ICurrentUserService _currentUserService;

        public Handler(IValidator<Query> validator, ApplicationDBContext dbContext, ICurrentUserService currentUserService)
        {
            _validator = validator;
            _dbContext = dbContext;
            _currentUserService = currentUserService;
        }

        public async Task<Result<GetProjectDetailsResponse>> Handle(Query request, CancellationToken cancellationToken)
        {
            try
            {
                var validationResult = await _validator.ValidateAsync(request, cancellationToken);
                if (!validationResult.IsValid)
                    return Result.Failure<GetProjectDetailsResponse>(new Error("GetProjectDetails.Validation", validationResult.ToString()));

                if (!await _currentUserService.IsProjectMember(request.ProjectId, cancellationToken))
                    return Result.Failure<GetProjectDetailsResponse>(new Error("GetProjectDetails.NoAccess", "Access denied"));

                var project = await GetProjectDetails(request.ProjectId, cancellationToken);

                return project != null
                    ? new GetProjectDetailsResponse() { Project = project }
                    : Result.Failure<GetProjectDetailsResponse>(new Error("GetProjectDetails.NotFounded",
                        "Project not founded"));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return Result.Failure<GetProjectDetailsResponse>(new Error("GetProjectDetails.Exception", e.ToString()));
            }
        }
        
        private async Task<ProjectDto?> GetProjectDetails(int projectId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Projects.Where(x => x.Id == projectId).Select(x => new ProjectDto()
            {
                Id = x.Id,
                Name = x.Name,
                ProjectTasks = x.ProjectTasks.Select(t=> new ProjectTaskListItemDto()
                {
                    Id = t.Id,
                    Name = t.Name,
                    TaskStatus = t.TaskStatus,
                    Contractor = t.UserProjectId.HasValue ?  new ProjectTaskContractorDto()
                    {
                        Email = t.UserProject!.User.Email,
                        UserFullName = t.UserProject!.User.FullName,
                        UserProjectId = t.UserProjectId.Value,
                    } : null
                }).ToList(),
                Participants = x.UserProjects.Select(p => new UserProjectDto()
                {
                    Id = p.Id,
                    Rank = p.Rank,
                    User = new UserDto()
                    {
                        Id = p.User.Id,
                        FullName = p.User.FullName,
                        Email = p.User.Email
                    },
                }).ToList()
            }).FirstOrDefaultAsync(cancellationToken);
        }
        
    }
}

public class GetProjectDetailsEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("api/projects/{id}", async (int projectId,  ISender sender) =>
        {
            var query = new GetProjectDetails.Query() {ProjectId = projectId};

            var result = await sender.Send(query);
            if (result.IsFailure)
                return Results.BadRequest(result.Error);

            return Results.Ok(result.Value);
        }).RequireAuthorization().WithTags("Projects");
    }
}