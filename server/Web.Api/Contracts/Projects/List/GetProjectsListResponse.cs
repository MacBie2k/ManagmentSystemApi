using Web.Api.Dtos.Project;

namespace Web.Api.Contracts.Projects.List;

public class GetProjectsListResponse
{
    public List<ProjectListItemDto> Projects { get; set; }
}