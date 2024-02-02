using Carter;
using FluentValidation;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Web.Api.Contracts.UserProjects.AddParticipant;
using Web.Api.Database;
using Web.Api.Entities;
using Web.Api.Extensions.CurrentUserService;
using Web.Api.Shared;

namespace Web.Api.Features.UserProjects;

public class AddParticipant
{
    public class Command : IRequest<Result<int>>
    {
        public int ProjectId { get; set; }
        public string Email { get; set; }
        public UserProjectRankEnum Rank { get; set; }
    }
    
    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.ProjectId).NotEmpty();
            RuleFor(c => c.Email).EmailAddress();
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
                    return Result.Failure<int>(new Error("AddParticipant.Validation", validationResult.ToString()));
                
                if (!await _currentUserService.CanModerateProject(request.ProjectId, cancellationToken))
                    return Result.Failure<int>(new Error("AddParticipant.NoAccess", "Access denied"));


                var userId = await _dbContext.Users
                    .Where(x => x.Email == request.Email)
                    .Select(x => x.Id)
                    .SingleOrDefaultAsync(cancellationToken);

                var userProject = new UserProject()
                {
                    ProjectId = request.ProjectId,
                    UserId = userId,
                    Rank = request.Rank
                };
                
                await _dbContext.UserProjects.AddAsync(userProject, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
                return userProject.Id;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return Result.Failure<int>(new Error("CreateProject.Exception", e.ToString()));
            }
        }
    }
}

public class AddProjectParticipantEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("api/userprojects", async (AddParticipantRequest request,  ISender sender) =>
        {
            var command = request.Adapt<AddParticipant.Command>();

            var result = await sender.Send(command);
            if (result.IsFailure)
                return Results.BadRequest(result.Error);

            return Results.Ok(result.Value);
        }).RequireAuthorization().WithTags("UserProjects");
    }
}