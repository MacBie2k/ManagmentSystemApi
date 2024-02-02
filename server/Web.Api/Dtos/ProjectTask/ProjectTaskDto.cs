using Web.Api.Dtos.Comments;
using Web.Api.Entities;

namespace Web.Api.Dtos.ProjectTask;

public class ProjectTaskDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public ProjectTaskContractorDto? Contractor { get; set; }
    public List<CommentDto> Comments { get; set; }
    public TaskStatusEnum TaskStatus { get; set; }
}