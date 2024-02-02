using Carter;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Web.Api.Database;
using Web.Api.Extensions.CurrentUserService;
using Web.Api.Shared;

namespace Web.Api.Features.Comments;

public class DeleteComment
{
    public class Command : IRequest<Result>
    {
        public int CommentId { get; set; }
    }
    
    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.CommentId).NotEmpty();
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
                    return Result.Failure<int>(new Error("DeleteComment.Validation", validationResult.ToString()));

                var projectId = await _dbContext.Comments.Where(x => x.Id == request.CommentId)
                    .Select(x => x.ProjectTask.ProjectId).FirstOrDefaultAsync(cancellationToken);

                var isCommentCreator = await _dbContext.Comments
                    .AnyAsync(x => x.Id == request.CommentId && x.UserProject.UserId == _currentUserService.UserId,
                        cancellationToken);
                
                if (!await _currentUserService.CanModerateProject(projectId, cancellationToken) && !isCommentCreator)
                    return Result.Failure<int>(new Error("DeleteComment.NoAccess", "Access denied"));

                await DeleteComment(request.CommentId, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
                return Result.Success();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return Result.Failure<int>(new Error("DeleteComment.Exception", e.ToString()));
            }
        }
        public async Task DeleteComment(int commentId, CancellationToken cancellationToken = default)
        {
            var comment = _dbContext.Comments.FirstOrDefault(x => x.Id == commentId);
            if (comment != null) 
                _dbContext.Comments.Remove(comment);
        }
    }
}

public class DeleteCommentEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete("api/comments/{id}", async (int commentId, ISender sender) =>
        {
            var command = new DeleteComment.Command() { CommentId = commentId };

            var result = await sender.Send(command);
            if (result.IsFailure)
                return Results.BadRequest(result.Error);

            return Results.Ok(result);
        }).RequireAuthorization().WithTags("Comments");
    }
}