using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Web.Api.Database;
using Web.Api.Entities;

namespace Web.Api.Extensions.CurrentUserService;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly List<Claim> _claimsIdentity;
    private readonly ApplicationDBContext _dbContext;
    public CurrentUserService(IHttpContextAccessor httpContextAccessor, ApplicationDBContext dbContext)
    {
        var identity = httpContextAccessor.HttpContext?.User.Identity as ClaimsIdentity;
        _claimsIdentity = identity!.Claims.ToList();
        _httpContextAccessor = httpContextAccessor;
        _dbContext = dbContext;
    }

    public bool IsAuthenticated
        => _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated
           ?? false;

    public Task<bool> IsProjectOwner(int projectId, CancellationToken cancellationToken = default)
    {
        var userId = Guid.Parse(_claimsIdentity[0].Value);
        return _dbContext.UserProjects
            .AnyAsync(x => x.UserId == userId
                           && x.ProjectId == projectId
                           && x.Rank == UserProjectRankEnum.Owner, cancellationToken);

    }

    public Task<bool> CanModerateProject(int projectId, CancellationToken cancellationToken = default)
    {
        var userId = Guid.Parse(_claimsIdentity[0].Value);
        return _dbContext.UserProjects
            .AnyAsync(x => x.UserId == userId
                           && x.ProjectId == projectId
                           && (x.Rank == UserProjectRankEnum.Owner || x.Rank == UserProjectRankEnum.Moderator), cancellationToken);
    }

    public Task<bool> IsProjectMember(int projectId, CancellationToken cancellationToken = default)
    {
        var userId = Guid.Parse(_claimsIdentity[0].Value);
        return _dbContext.UserProjects
            .AnyAsync(x => x.UserId == userId
                           && x.ProjectId == projectId, cancellationToken);
    }

    public Task<bool> IsUserProjectOwner(int userProjectId, CancellationToken cancellationToken = default)
    {
        var userId = Guid.Parse(_claimsIdentity[0].Value);
        return _dbContext.UserProjects
            .AnyAsync(x => x.UserId == userId
                           && x.Id == userProjectId, cancellationToken);
    }

    public Guid UserId
    {
        get
        {
            var value = _claimsIdentity[0].Value;
            return Guid.Parse(value);
        }
    }

    public string UserEmail{
        get
        {
            var value = _claimsIdentity[1].Value;
            return value ?? "";
        }
    }
}