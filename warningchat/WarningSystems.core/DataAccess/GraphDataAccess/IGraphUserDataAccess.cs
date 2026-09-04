using Microsoft.Graph.Models;
using WarningSystems.Core.ViewModels;

namespace WarningSystems.Core.DataAccess.GraphDataAccess;

public interface IGraphUserDataAccess
{
    public Task<IReadOnlyList<string>> GetMatchedGroupIdsAsync(string userObjectId, IEnumerable<string> groupIdsToCheck);

    public Task<User?> GetMeAsync();

    public Task<User?> GetUserAsync(string userIdOrUpn);

    public Task<List<User>> GetUsersUnderMeAsync();

    public Task<User?> GetManagerAsync(string userId);

    public Task<List<User>> GetDirectReportsUsersAsync(string userId);

    public Task<List<User>> GetUsersBelowMyLevelAsync();

    public Task SendEmailAsync(Message message, bool saveToSentItems = true);
    public Task SendEmailAsync(SendEmailRequest request);
}