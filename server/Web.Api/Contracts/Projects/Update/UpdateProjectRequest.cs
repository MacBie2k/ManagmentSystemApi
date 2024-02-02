namespace Web.Api.Contracts.Projects.Update;

public class UpdateProjectRequest
{
    public int ProjectId { get; set; }
    public string Name { get; set; }
    public string Description { get; set;}
}