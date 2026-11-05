using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Serilog;
using System.Data;
using WarningSystems.core.DataAccess.ADUsersDataAccess.Context;
using WarningSystems.core.DataAccess.ADUsersDataAccess.Context.Entities;

namespace WarningSystems.core.DataAccess.ADUsersDataAccess;

public class ADUsersDataAccess : IADUsersDataAccess
{

    private readonly ADUsersDbContext _context;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(6);

    public ADUsersDataAccess(
        ADUsersDbContext context,
        IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<ADUser?> GetUserAsync(string userIdOrUpn)
    {
        if (string.IsNullOrWhiteSpace(userIdOrUpn))
        {
            return null;
        }

        var identifier = userIdOrUpn.Trim();

        var isGuid = Guid.TryParse(identifier, out var userGuid);

        var cacheIdentifier = isGuid
            ? userGuid.ToString("D")
            : identifier.ToLowerInvariant();

        var cacheKey = $"ADUsers:User:{cacheIdentifier}";

        return await GetCachedAsync<ADUser?>(
            cacheKey,
            nameof(GetUserAsync),
            () => isGuid
                ? _context.ADUsers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.ID_Guid == userGuid)
                : _context.ADUsers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        u => u.UserPrincipalName == identifier));
    }

    public async Task<ADUser?> GetManagerAsync(string userId)
    {
        if (!Guid.TryParse(userId, out var userGuid))
        {
            return null;
        }

        var cacheKey = $"ADUsers:Manager:{userGuid:D}";

        return await GetCachedAsync<ADUser?>(
            cacheKey,
            nameof(GetManagerAsync),
            async () =>
            {
                var user = await GetUserAsync(userGuid.ToString("D"));

                if (!Guid.TryParse(user?.Manager_ID, out var managerGuid))
                {
                    return null;
                }

                return await GetUserAsync(managerGuid.ToString("D"));
            });
    }

    public async Task<List<ADUser>> GetDirectReportsUsersAsync(
        string userId)
    {
        if (!Guid.TryParse(userId, out var userGuid))
        {
            return [];
        }

        var cacheKey = $"ADUsers:DirectReports:{userGuid:D}";

        var users = await GetCachedAsync(
            cacheKey,
            nameof(GetDirectReportsUsersAsync),
            () =>
            {
                var managerId = userGuid.ToString("D");

                return _context.ADUsers
                    .AsNoTracking()
                    .Where(u => u.Manager_ID == managerId)
                    .ToListAsync();
            });

        return users.ToList();
    }

    public async Task<List<ADUser>> GetUsersBelowMyLevelAsync(string userId)
    {
        // Validate the current user's ID.
        if (!Guid.TryParse(userId, out var userGuid))
        {
            return [];
        }

        var cacheKey = $"ADUsers:UsersBelowMyLevel:{userGuid:D}";
        var result = await GetCachedAsync(
            cacheKey,
            nameof(GetUsersBelowMyLevelAsync),
            async () =>
            {
                //  Get my user details.
                var me = await GetUserAsync(userGuid.ToString("D"));

                if (me?.ID_Guid is not Guid myId)
                {
                    return new List<ADUser>();
                }
                var users = new Dictionary<Guid, ADUser>();
                var visitedManagers = new HashSet<Guid>();

                //  Get users directly under me.
                await AddReportsRecursiveAsync( myId, users, visitedManagers);

                // Get my manager.
                var manager = await GetManagerAsync(myId.ToString("D"));

                if (manager?.ID_Guid is Guid managerId)
                {
                    //  Get everyone who reports directly to my manager.
                    var colleagues = await GetDirectReportsUsersAsync(
                        managerId.ToString("D"));

                    // Find colleagues at the same level as me.
                    var sameLevelUsers = colleagues.Where(user =>
                        string.Equals(
                            user.Department?.Trim(),
                            me.Department?.Trim(),
                            StringComparison.OrdinalIgnoreCase)
                        &&
                        string.Equals(
                            user.JobTitle?.Trim(),
                            me.JobTitle?.Trim(),
                            StringComparison.OrdinalIgnoreCase));

                    foreach (var colleague in sameLevelUsers)
                    {
                        // Skip myself and users without an ID.
                        if (colleague.ID_Guid is not Guid colleagueId
                            || colleagueId == myId)
                        {
                            continue;
                        }

                        // Get users directly under this same-level colleague.
                        await AddReportsRecursiveAsync(
                            colleagueId,
                            users,
                            visitedManagers);
                    }
                }

                //Ensure I am excluded from the final result.
                users.Remove(myId);


                return users.Values
                    .Where(user => user.AccountEnabled == true)
                    .OrderBy(user => user.DisplayName)
                    .ThenBy(user => user.UserPrincipalName)
                    .ToList();
            });

        return result.ToList();
    }
    private async Task AddReportsRecursiveAsync(Guid managerId, Dictionary<Guid, ADUser> users, HashSet<Guid> visitedManagers)
    {

        if (!visitedManagers.Add(managerId))
        {
            return;
        }

        var directReports = await GetDirectReportsUsersAsync(managerId.ToString("D"));

        foreach (var user in directReports)
        {
            if (user.ID_Guid is not Guid userId)
            {
                continue;
            }

            users[userId] = user;

            await AddReportsRecursiveAsync(userId, users, visitedManagers);
        }
    }

    private async Task<T> GetCachedAsync<T>(string cacheKey,string methodName,Func<Task<T>> load)
    {
        if (_cache.TryGetValue<T>(cacheKey, out var cached))
        {
            Log.Information(  "{MethodName}. Cache hit: true. Key: {CacheKey}", methodName,  cacheKey);

            return cached!;
        }

        try
        {
            var result = await load();

            _cache.Set(cacheKey, result, CacheDuration);

            Log.Information( "{MethodName}. Cache hit: false. Key: {CacheKey}",methodName,cacheKey);

            return result;
        }
        catch (Exception ex)
        {
            Log.Error( ex, "{MethodName} failed. Key: {CacheKey}", methodName,cacheKey);

            throw;
        }
    }
}