using WarningSystems.core.DataAccess.ADUsersDataAccess.Context.Entities;

namespace WarningSystems.core.DataAccess.ADUsersDataAccess;

public interface IADUsersDataAccess
{
    Task<ADUser?> GetUserAsync(string userIdOrUpn);

    Task<ADUser?> GetManagerAsync(string userId);

    Task<List<ADUser>> GetDirectReportsUsersAsync(string userId);

    Task<List<ADUser>> GetUsersBelowMyLevelAsync(string userId);
}