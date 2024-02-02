using Carter;
using FluentValidation;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Web.Api.Contracts.Comments.Update;
using Web.Api.Database;
using Web.Api.Entities;
using Web.Api.Extensions.CurrentUserService;
using Web.Api.Shared;

namespace Web.Api.Features.Comments;

public class UpdateComment
{
    public class Command : IRequest<Result>
    {
        public int CommentId { get; set; }
        public string Content { get; set; }
    }
    
    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.CommentId).NotEmpty();
            RuleFor(x => x.Content).MaximumLength(Entity.DescriptionMaxLength);
        }
    }
    
    internal sealed class Handler : IRequestHandler<Command, Result>
    {
        private readonly ApplicationDBContext _dbContext;
        private readonly ICurrentUserService _currentUserService;
        private readonly IValidator<Command> _validator;

        public Handler(ApplicationDBContext dbContext, ICurrentUserService currentUserService, IValidator<Command> validator)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
            _validator = validator;
        }

        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            try
            {
                var validationResult = await _validator.ValidateAsync(request, cancellationToken);
                if (!validationResult.IsValid)
                    return Result.Failure(new Error("UpdateComment.Validation", validationResult.ToString()));

                var userProjectId = await _dbContext.Comments
                    .Where(x => x.Id == request.CommentId && x.UserProject.UserId == _currentUserService.UserId)
                    .Select(x => x.UserProjectId).FirstOrDefaultAsync(cancellationToken);
                
                if (userProjectId == default)
                    return Result.Failure(new Error("UpdateComment.NoAccess", "Access denied"));
                
                var success = await UpdateComment(request.CommentId, request.Content, cancellationToken);
                
                if (!success)
                    return Result.Failure(new Error("UpdateComment.NoAccess", "Access denied"));
                
                await _dbContext.SaveChangesAsync(cancellationToken);
                return Result.Success();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return Result.Failure(new Error("UpdateProjectTask.Excetpion", e.ToString()));
            }
        }
        
        public async Task<bool> UpdateComment(int commentId, string content, CancellationToken cancellationToken = default)
        {
            var comment = _dbContext.Comments.FirstOrDefault(x => x.Id == commentId);

            if (comment != null)
            {
                comment.Content = content;
                _dbContext.Comments.Update(comment);
                return true;
            }

            return false;
        }
    }
}

public class UpdateCommentTaskEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("api/comments/{id}", async (UpdateCommentRequest request, ISender sender) =>
        {
            var command = request.Adapt<UpdateComment.Command>();

            var result = await sender.Send(command);
            if (result.IsFailure)
                return Results.BadRequest(result.Error);

            return Results.Ok(result);
        }).RequireAuthorization().WithTags("Comments");
    }
}