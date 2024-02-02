namespace Web.Api.Entities;

public class Comment : Entity<int>
{
    public int? UserProjectId { get; set; }
    public UserProject? UserProject { get; set; }
    public int ProjectTaskId { get; set; }
    public ProjectTask ProjectTask { get; set; }
    public string Content { get; set; }
}