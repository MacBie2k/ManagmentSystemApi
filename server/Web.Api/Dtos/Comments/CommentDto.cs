namespace Web.Api.Dtos.Comments;

public class CommentDto
{
    public int Id { get; set; }
    public int? UserProjectId { get; set; }
    public int ProjectTaskId { get; set; }
    public string Content { get; set; }
}