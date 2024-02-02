using Web.Api.Entities;

namespace Web.Api.Contracts.ProjectTasks.UpdateProjectTaskStatus;

public class UpdateProjectTaskStatusRequest
{
    public int ProjectTaskId { get; set; }
    public TaskStatusEnum TaskStatus { get; set; }
}