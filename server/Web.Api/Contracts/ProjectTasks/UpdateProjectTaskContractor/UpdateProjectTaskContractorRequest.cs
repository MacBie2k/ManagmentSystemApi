namespace Web.Api.Contracts.ProjectTasks.UpdateProjectTaskContractor;

public class UpdateProjectTaskContractorRequest
{
    public int ProjectTaskId { get; set; }
    public int? UserProjectId { get; set; }
}