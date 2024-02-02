using Web.Api.Entities;

namespace Web.Api.Dtos.ProjectTask;

public class ProjectTaskListItemDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public ProjectTaskContractorDto? Contractor { get; set; }
    public TaskStatusEnum TaskStatus { get; set; }
}