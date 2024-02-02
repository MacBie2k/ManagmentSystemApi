using Web.Api.Entities;

namespace Web.Api.Contracts.UserProjects.AddParticipant;

public class AddParticipantRequest
{
    public int ProjectId { get; set; }
    public string Email { get; set; }
    public UserProjectRankEnum Rank { get; set; }
}