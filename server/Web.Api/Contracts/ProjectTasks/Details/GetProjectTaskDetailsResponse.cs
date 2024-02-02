using Web.Api.Dtos.ProjectTask;

namespace Web.Api.Contracts.ProjectTasks.Details;

public class GetProjectTaskDetailsResponse
{
    public ProjectTaskDto ProjectTask { get; set; }
}