namespace Web.Api.Entities;

public class Project : Entity<int>
{
    public string Name { get; set; }
    public string Description { get; set; }
    public virtual List<UserProject> UserProjects { get; set; }
    public virtual List<ProjectTask> ProjectTasks { get; set; }
}