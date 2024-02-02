using Carter;
using FluentValidation;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Web.Api.Contracts.Comments.Create;
using Web.Api.Contracts.ProjectTasks.Create;
using Web.Api.Database;
using Web.Api.Entities;
using Web.Api.Extensions.CurrentUserService;
using Web.Api.Features.ProjectTasks;
using Web.Api.Shared;

namespace Web.Api.Features.Comments;

public class CreateComment
{
    public class Command : IRequest<Result<int>>
    {
        public int ProjectTaskId { get; set; }
        public string Content { get; set; }
    }
    
    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.ProjectTaskId).NotEmpty();
            RuleFor(x => x.Content).MaximumLength(Entity.DescriptionMaxLength);
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
                    return Result.Failure<int>(new Error("CreateComment.Validation", validationResult.ToString()));

                var projectId = await _dbContext.ProjectTasks
                    .Where(x => x.Id == request.ProjectTaskId)
                    .Select(x => x.ProjectId).FirstOrDefaultAsync(cancellationToken);
                
                var user = _currentUserService.UserId;
                
                if (!await _currentUserService.IsProjectMember(projectId, cancellationToken))
                    return Result.Failure<int>(new Error("CreateComment.NoAccess", "Access denied"));

                var userProjectId = await _dbContext.UserProjects
                    .Where(x => x.ProjectId == projectId && x.UserId == user)
                    .Select(x => x.Id).FirstOrDefaultAsync(cancellationToken);

                var comment = new Comment()
                {
                    ProjectTaskId = request.ProjectTaskId,
                    UserProjectId = userProjectId,
                    Content = request.Content
                };
                    await _dbContext.Comments.AddAsync(comment, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
                return comment.Id;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return Result.Failure<int>(new Error("CreateComment.Exception", e.ToString()));
            }
        }
    }
}

public class CreateCommentTaskEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("api/comments", async (CreateCommentRequest request, ISender sender) =>
        {
            var command = request.Adapt<CreateComment.Command>();

            var result = await sender.Send(command);
            if (result.IsFailure)
                return Results.BadRequest(result.Error);

            return Results.Ok(result.Value);
        }).RequireAuthorization().WithTags("Comments");
    }
}