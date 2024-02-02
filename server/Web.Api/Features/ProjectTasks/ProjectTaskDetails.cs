using Carter;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Web.Api.Contracts.ProjectTasks.Details;
using Web.Api.Database;
using Web.Api.Dtos.Comments;
using Web.Api.Dtos.ProjectTask;
using Web.Api.Extensions.CurrentUserService;
using Web.Api.Shared;

namespace Web.Api.Features.ProjectTasks;

public class ProjectTaskDetails
{
    public class Query : IRequest<Result<GetProjectTaskDetailsResponse>>
    {
        public int ProjectTaskId { get; set; }
    }
    
    public class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ProjectTaskId).NotEmpty();
        }
    }
    
    internal sealed class Handler : IRequestHandler<Query, Result<GetProjectTaskDetailsResponse>>
    {
        private readonly ApplicationDBContext _dbContext;
        private readonly IValidator<Query> _validator;
        private readonly ICurrentUserService _currentUserService;

        public Handler(ApplicationDBContext dbContext, IValidator<Query> validator, ICurrentUserService currentUserService)
        {
            _dbContext = dbContext;
            _validator = validator;
            _currentUserService = currentUserService;
        }

        public async Task<Result<GetProjectTaskDetailsResponse>> Handle(Query request, CancellationToken cancellationToken)
        {
            try
            {
                var validationResult = await _validator.ValidateAsync(request, cancellationToken);
                if (!validationResult.IsValid)
                    return Result.Failure<GetProjectTaskDetailsResponse>(new Error("ProjectTaskDetails.Validation", validationResult.ToString()));
    
                var projectId = await _dbContext.ProjectTasks.Where(x => x.Id == request.ProjectTaskId)
                    .Select(x => x.ProjectId).FirstOrDefaultAsync(cancellationToken);

                if (!await _currentUserService.IsProjectMember(projectId, cancellationToken))
                    return Result.Failure<GetProjectTaskDetailsResponse>(new Error("ProjectTaskDetails.NoAccess", "Access denied"));

                var projectTask = await GetProjectTaskDetails(request.ProjectTaskId, cancellationToken);

                return projectTask != null
                    ? new GetProjectTaskDetailsResponse() { ProjectTask = projectTask }
                    : Result.Failure<GetProjectTaskDetailsResponse>(new Error("ProjectTaskDetails.NotFounded",
                        "ProjectTask not founded"));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return Result.Failure<GetProjectTaskDetailsResponse>(new Error("ProjectTaskDetails.Exception", e.ToString()));
            }
        }
        private async Task<ProjectTaskDto?> GetProjectTaskDetails(int projectTaskId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.ProjectTasks.Where(x => x.Id == projectTaskId).Select(t => new ProjectTaskDto()
            {
                Id = t.Id,
                Name = t.Name,
                TaskStatus = t.TaskStatus,
                Description = t.Description,
                Comments = t.Comments.Select(c=> new CommentDto()
                {
                    Id = c.Id,
                    Content = c.Content,
                    UserProjectId = c.UserProjectId,
                    ProjectTaskId = c.ProjectTaskId
                }).ToList(),
                Contractor = t.UserProjectId.HasValue ?  new ProjectTaskContractorDto()
                {
                    Email = t.UserProject!.User.Email,
                    UserFullName = t.UserProject!.User.FullName,
                    UserProjectId = t.UserProjectId.Value,
                } : null
            }).FirstOrDefaultAsync(cancellationToken);
        }
    }
}

public class GetProjectDetailsEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("api/projecttasks/{id}", async (int projectTaskId,  ISender sender) =>
        {
            var query = new ProjectTaskDetails.Query() {ProjectTaskId = projectTaskId};

            var result = await sender.Send(query);
            if (result.IsFailure)
                return Results.BadRequest(result.Error);

            return Results.Ok(result.Value);
        }).RequireAuthorization().WithTags("ProjectTasks");
    }
}