namespace Web.Api.Entities;

public class ProjectTask : Entity<int>
{
    public string Name { get; set; }
    public string Description { get; set; }
    public int ProjectId { get; set; }
    public virtual Project Project { get; set; }
    public int? UserProjectId { get; set; }
    public virtual UserProject? UserProject { get; set; }
    public virtual List<Comment> Comments { get; set; }
    public TaskStatusEnum TaskStatus { get; set; }
}