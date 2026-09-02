using Microsoft.Extensions.Configuration;
using WarningSystems.Core.Auth.Interface;
using WarningSystems.Core.DataAccess.GraphDataAccess;
using WarningSystems.Core.Services.Interface;

namespace WarningSystems.Core.Services;

public class UserRoleService : IUserRoleService
{
    private readonly IConfiguration _config;
    private readonly ICurrentUserAccessor _me;
    private readonly IGraphUserDataAccess _groups;

    public UserRoleService(IConfiguration config, ICurrentUserAccessor me, IGraphUserDataAccess groups)
    {
        _config = config;
        _me = me;
        _groups = groups;
    }

    public async Task<IReadOnlyList<string>> GetCurrentUserRolesAsync()
    {
        if (!_me.IsAuthenticated || string.IsNullOrWhiteSpace(_me.ObjectId))
            return Array.Empty<string>();

        var roleGroups = _config.GetSection("Roles").Get<Dictionary<string, string>>()
                        ?? new Dictionary<string, string>();

        var exclusions = _config.GetSection("RoleExclusions").Get<List<string>>()
                        ?? new List<string>();

        var matchedGroupIds = await _groups.GetMatchedGroupIdsAsync(_me.ObjectId, roleGroups.Values);

        var matchedSet = new HashSet<string>(matchedGroupIds, StringComparer.OrdinalIgnoreCase);

        var roles = roleGroups
            .Where(kvp => matchedSet.Contains(kvp.Value))
            .Select(kvp => kvp.Key)
            .ToList();

        roles.RemoveAll(r => exclusions.Contains(r, StringComparer.OrdinalIgnoreCase));

        return roles;
    }

    public async Task<bool> CurrentUserIsInRoleAsync(string role)
    {
        var roles = await GetCurrentUserRolesAsync();
        return roles.Contains(role, StringComparer.OrdinalIgnoreCase);
    }
}