namespace Web.Api.Contracts.Comments.Create;

public class CreateCommentRequest
{
    public int ProjectTaskId { get; set; }
    public string Content { get; set; }
}