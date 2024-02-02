using Web.Api.Entities;

namespace Web.Api.Contracts.UserProjects.UpdateParticipantRole;

public class UpdateParticipantRankRequest
{
    public int UserProjectId { get; set; }
    public UserProjectRankEnum Rank { get; set; }
}