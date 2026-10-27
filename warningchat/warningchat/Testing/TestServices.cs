using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Graph.Models;
using WarningSystems.Core.Auth.Interface;
using WarningSystems.Core.DataAccess.AzureFileStorage;
using WarningSystems.Core.DataAccess.GraphDataAccess;
using WarningSystems.Core.DataAccess.SOPDataAccess;
using WarningSystems.Core.DataAccess.SOPDataAccess.Context.Entity;
using WarningSystems.Core.Services.Interface;
using WarningSystems.Core.ViewModels;

namespace WarningSystems.Testing;

public static class TestServices
{
    public static void AddWarningSystemTestServices(this IServiceCollection services, IConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration["WarningSystemTesting:Key"]) || configuration["WarningSystemTesting:Key"]!.Length < 32)
            throw new InvalidOperationException("Aspire must supply a per-run test authentication key.");
        services.RemoveAll<IGraphUserDataAccess>();
        services.AddScoped<IGraphUserDataAccess, TestGraph>();
        services.RemoveAll<IUserRoleService>();
        services.AddScoped<IUserRoleService, TestRoles>();
        services.RemoveAll<ISOPDataAccess>();
        services.AddSingleton<ISOPDataAccess, TestSop>();
        services.RemoveAll<IAzureFileStorageDataAccess>();
        services.AddSingleton<IAzureFileStorageDataAccess, TestFileStorage>();
    }
}

internal sealed class TestRoles(IHttpContextAccessor accessor) : IUserRoleService
{
    public Task<IReadOnlyList<string>> GetCurrentUserRolesAsync() => Task.FromResult<IReadOnlyList<string>>(
        accessor.HttpContext?.User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray() ?? []);
    public async Task<bool> CurrentUserIsInRoleAsync(string role) =>
        (await GetCurrentUserRolesAsync()).Contains(role, StringComparer.OrdinalIgnoreCase);
}

internal sealed class TestSop : ISOPDataAccess
{
    public Task<List<SopDocument>> GetActiveDocumentsAsync() => Task.FromResult(new List<SopDocument>());
}

// No Graph, Azure AD, mail, or external SOP requests are made by the login checks.
internal sealed class TestGraph(ICurrentUserAccessor me) : IGraphUserDataAccess
{
    private static readonly User Employee = new() { Id = "test-employee", DisplayName = "Test Employee", Department = "Test Department", Mail = "employee@example.test" };
    public Task<IReadOnlyList<string>> GetMatchedGroupIdsAsync(string userObjectId, IEnumerable<string> groups) => Task.FromResult<IReadOnlyList<string>>([]);
    public Task<User?> GetMeAsync() => Task.FromResult<User?>(new User { Id = me.ObjectId, DisplayName = me.DisplayName, Mail = me.UpnOrEmail });
    public Task<User?> GetUserAsync(string id) => Task.FromResult<User?>(id == Employee.Id ? Employee : new User { Id = id, DisplayName = id == me.ObjectId ? me.DisplayName : id });
    public Task<List<User>> GetUsersUnderMeAsync() => Task.FromResult(new List<User> { Employee });
    public Task<List<User>> GetUsersBelowMyLevelAsync() => GetUsersUnderMeAsync();
    public Task<User?> GetManagerAsync(string userId) => GetMeAsync();
    public Task<List<User>> GetDirectReportsUsersAsync(string userId) => GetUsersUnderMeAsync();
    public Task SendEmailAsync(Message message, bool saveToSentItems = true) => throw new InvalidOperationException("Mail is disabled for login-only tests.");
    public Task SendEmailAsync(SendEmailRequest request) => throw new InvalidOperationException("Mail is disabled for login-only tests.");
}
