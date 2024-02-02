namespace Web.Api.Contracts.ProjectTasks.Update;

public class UpdateProjectTaskRequest
{
    public int ProjectTaskId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
}