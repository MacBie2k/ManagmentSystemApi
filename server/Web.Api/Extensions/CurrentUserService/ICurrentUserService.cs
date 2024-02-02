namespace Web.Api.Extensions.CurrentUserService;

public interface ICurrentUserService
{
    public Guid UserId { get; }

    public string UserEmail { get; }
    
    public bool IsAuthenticated { get; }
    
    public Task<bool> IsProjectOwner(int projectId, CancellationToken cancellationToken = default);
    
    public Task<bool> CanModerateProject(int projectId, CancellationToken cancellationToken = default);
    
    public Task<bool> IsProjectMember(int projectId, CancellationToken cancellationToken = default);

    public Task<bool> IsUserProjectOwner(int userProjectId, CancellationToken cancellationToken = default);
}