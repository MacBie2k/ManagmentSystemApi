namespace Web.Api.Contracts.Comments.Update;

public class UpdateCommentRequest
{
    public int CommentId { get; set; }
    public string Content { get; set; }
}