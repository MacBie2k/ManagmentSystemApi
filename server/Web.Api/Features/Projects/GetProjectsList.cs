using Carter;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web.Api.Contracts.Projects.Details;
using Web.Api.Contracts.Projects.List;
using Web.Api.Database;
using Web.Api.Dtos.Project;
using Web.Api.Extensions.CurrentUserService;
using Web.Api.Shared;

namespace Web.Api.Features.Projects;

public class GetProjectsList
{
    public class Query : IRequest<Result<GetProjectsListResponse>>
    {
        
    }
    internal sealed class Handler : IRequestHandler<Query, Result<GetProjectsListResponse>>
    {
        private readonly ApplicationDBContext _dbContext;
        private readonly ICurrentUserService _currentUserService;

        public Handler(ApplicationDBContext dbContext, ICurrentUserService currentUserService)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
        }

        public async Task<Result<GetProjectsListResponse>> Handle(Query request, CancellationToken cancellationToken)
        {
            try
            {
                return new GetProjectsListResponse
                {
                    Projects = await _dbContext.UserProjects.Where(x => x.UserId == _currentUserService.UserId)
                        .Select(x => new ProjectListItemDto()
                        {
                            Id = x.Project.Id,
                            Name = x.Project.Name
                        }).ToListAsync(cancellationToken)
                };
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return Result.Failure<GetProjectsListResponse>(new Error("GetProjectsList.Exception", e.ToString()));
            }
        }

    }
}

public class GetProjectsListEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("api/projects", async (ISender sender) =>
        {
            var query = new GetProjectsList.Query();

            var result = await sender.Send(query);
            if (result.IsFailure)
                return Results.BadRequest(result.Error);

            return Results.Ok(result.Value);
        }).RequireAuthorization().WithTags("Projects").WithTags("Projects");
    }
}