using Web.Api.Dtos.Project;
using Web.Api.Dtos.Users;
using Web.Api.Entities;

namespace Web.Api.Dtos.UserProjects;

public class UserProjectDto
{
    public int Id { get; set; }
    public UserProjectRankEnum Rank { get; set; }
    public UserDto User { get; set; }
}