using Web.Api.Entities;

namespace Web.Api.Contracts.ProjectTasks.Create;

public class CreateProjectTaskRequest
{
    public string Name { get; set; }
    public string Description { get; set; }
    public TaskStatusEnum TaskStatus { get; set; }
    public int ProjectId { get; set; }
}