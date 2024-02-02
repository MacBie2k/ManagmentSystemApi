using Carter;
using FluentValidation;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Web.Api.Contracts.UserProjects.UpdateParticipantRole;
using Web.Api.Database;
using Web.Api.Entities;
using Web.Api.Extensions.CurrentUserService;
using Web.Api.Shared;

namespace Web.Api.Features.UserProjects;

public class UpdateParticipantRank
{
    public class Command : IRequest<Result>
    {
        public int UserProjectId { get; set; }
        public UserProjectRankEnum Rank { get; set; }
    }
    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.UserProjectId).NotEmpty();
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
                    return Result.Failure(new Error("UpdateParticipantRank.Validation", validationResult.ToString()));


                var projectId = await _dbContext.UserProjects.Where(x => x.Id == request.UserProjectId)
                    .Select(x => x.ProjectId).SingleOrDefaultAsync(cancellationToken);
                
                if (!await _currentUserService.IsProjectOwner(projectId, cancellationToken))
                    return Result.Failure(new Error("UpdateParticipantRank.NoAccess", "Access denied"));
                
                var success = await UpdateUserProject(request.UserProjectId, request.Rank, cancellationToken);
                
                if(!success)
                    return Result.Failure(new Error("UpdateParticipantRank.NoAccess", "Access denied"));
                
                await _dbContext.SaveChangesAsync(cancellationToken);
                return Result.Success();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return Result.Failure(new Error("UpdateParticipantRank", e.ToString()));
            }
        }
        
        public async Task<bool> UpdateUserProject(int userProjectId, UserProjectRankEnum rank, CancellationToken cancellationToken = default)
        {
            var userProject = _dbContext.UserProjects
                .SingleOrDefault(x => x.Id == userProjectId && x.UserId != _currentUserService.UserId);

            if (userProject != null)
            {
                userProject.Rank = rank;
                _dbContext.UserProjects.Update(userProject);
                return true;
            }

            return false;
        }
        
    }
}
public class UpdateParticipantRankEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("api/userprojects/{id}", async (UpdateParticipantRankRequest request, ISender sender) =>
        {
            var command = request.Adapt<UpdateParticipantRank.Command>();

            var result = await sender.Send(command);
            if (result.IsFailure)
                return Results.BadRequest(result.Error);

            return Results.Ok(result);
        }).RequireAuthorization().WithTags("UserProjects");
    }
}