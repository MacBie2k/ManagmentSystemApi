using Carter;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Web.Api.Database;
using Web.Api.Entities;
using Web.Api.Extensions.CurrentUserService;
using Web.Api.Shared;

namespace Web.Api.Features.UserProjects;

public class DeleteParticipant
{
    public class Command : IRequest<Result>
    {
        public int UserProjectId { get; set; }
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
                    return Result.Failure(new Error("DeleteParticipant.Validation", validationResult.ToString()));


                var projectId = await _dbContext.UserProjects.Where(x => x.Id == request.UserProjectId)
                    .Select(x => x.ProjectId).SingleOrDefaultAsync(cancellationToken);
                
                if (! await CanDeleteUserProject(request.UserProjectId, cancellationToken))
                    return Result.Failure(new Error("DeleteParticipant.NoAccess", "Access denied"));
                
                var success = await DeleteUserProject(request.UserProjectId, cancellationToken);
                
                if(!success)
                    return Result.Failure(new Error("DeleteParticipant.NoAccess", "Access denied"));
                
                await _dbContext.SaveChangesAsync(cancellationToken);
                return Result.Success();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return Result.Failure(new Error("DeleteParticipant", e.ToString()));
            }
        }


        public async Task<bool> CanDeleteUserProject(int userProjectId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.UserProjects.AnyAsync(
                x => (x.Id == userProjectId && x.UserId == _currentUserService.UserId &&
                     x.Rank != UserProjectRankEnum.Owner) || x.Project.UserProjects.Any(p=> p.Id != userProjectId && p.UserId == _currentUserService.UserId && p.Rank == UserProjectRankEnum.Owner), cancellationToken);
        }
        
        public async Task<bool> DeleteUserProject(int userProjectId, CancellationToken cancellationToken = default)
        {
            var userProject = _dbContext.UserProjects
                .SingleOrDefault(x => x.Id == userProjectId);

            if (userProject != null)
            {
                _dbContext.UserProjects.Remove(userProject);
                return true;
            }

            return false;
        }
    }
}

public class DeleteParticipantEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete("api/userprojects/{id}", async (int userProjectId, ISender sender) =>
        {
            var command = new DeleteParticipant.Command()
            {
                UserProjectId = userProjectId
            };

            var result = await sender.Send(command);
            if (result.IsFailure)
                return Results.BadRequest(result.Error);

            return Results.Ok(result);
        }).RequireAuthorization().WithTags("UserProjects");
    }
}