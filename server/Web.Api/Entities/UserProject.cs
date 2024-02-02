namespace Web.Api.Entities;

public class UserProject : Entity<int>
{
    public Guid UserId { get; set; }
    public virtual User User { get; set; }
    public int ProjectId { get; set; }
    public virtual Project Project { get; set; }
    public UserProjectRankEnum Rank { get; set; }
    public virtual List<ProjectTask> ProjectTasks { get; set; }
    public virtual List<Comment> Comments { get; set; }
}