using WarningSystems.core.DataAccess.ADUsersDataAccess;
using WarningSystems.core.DataAccess.ADUsersDataAccess.Context.Entities;
using System.Security.Claims;
using System.Collections.Concurrent;
using WarningSystems.core.Models.DTO;
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
        services.RemoveAll<IADUsersDataAccess>();
        services.AddScoped<IADUsersDataAccess, TestDirectory>();
        services.AddSingleton<TestOutbox>();
        services.PostConfigure<WarningSystems.Core.ViewModels.EmailSettings>(options => options.LegalRecipients = ["legal@example.test"]);
        services.AddOptions<ApplicationOptions>().Configure<IHttpContextAccessor>((options, accessor) =>
        {
            var request = accessor.HttpContext?.Request;
            if (request is not null) options.BaseUrl = $"{request.Scheme}://{request.Host}";
        });
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
    public Task<List<SopDocument>> GetActiveDocumentsAsync() => Task.FromResult(new List<SopDocument> {
        new() { SOPDocumentId = 9001, SOPDocumentCategoryId = 1, DocumentName = "UI Safety SOP", DocumentNumber = 1, VersionNumber = "1", Status = 1 }
    });
}

// External systems stay local in test builds. The warning manager and SQL access
// remain real; outgoing mail is recorded so workflow tests can inspect it.
internal sealed class TestGraph(ICurrentUserAccessor me, TestOutbox outbox) : IGraphUserDataAccess
{
    private static readonly User[] Employees = [
        new() { Id = "test-employee", DisplayName = "Test Employee", Department = "Test Department", Mail = "employee@example.test" },
        new() { Id = "test-employee-alice", DisplayName = "UI Alice", Department = "Sales", Mail = "alice@example.test" },
        new() { Id = "test-employee-bob", DisplayName = "UI Bob", Department = "Support", Mail = "bob@example.test" },
        new() { Id = "test-employee-cara", DisplayName = "UI Cara", Department = "Sales", Mail = "cara@example.test" }
    ];
    public Task<IReadOnlyList<string>> GetMatchedGroupIdsAsync(string userObjectId, IEnumerable<string> groups) => Task.FromResult<IReadOnlyList<string>>([]);
    public Task<User?> GetMeAsync() => GetUserAsync(me.ObjectId ?? "test-user");
    public Task<User?> GetUserAsync(string id)
    {
        var employee = Employees.FirstOrDefault(employee => employee.Id == id);
        if (employee is not null) return Task.FromResult<User?>(employee);
        var role = id switch { "test-user" => "User", "test-legal" => "Legal", "test-admin" => "Admin", "test-chairperson" => "Chairperson", _ => null };
        return Task.FromResult<User?>(new User { Id = id, DisplayName = role is null ? id : $"Test {role}", Mail = role is null ? null : $"{role.ToLowerInvariant()}@example.test" });
    }
    public Task<List<User>> GetUsersUnderMeAsync() => Task.FromResult(Employees.ToList());
    public Task<List<User>> GetUsersBelowMyLevelAsync() => GetUsersUnderMeAsync();
    public Task<User?> GetManagerAsync(string userId) => GetMeAsync();
    public Task<List<User>> GetDirectReportsUsersAsync(string userId) => GetUsersUnderMeAsync();
    public Task SendEmailAsync(Message message, bool saveToSentItems = true)
    {
        outbox.Add(new(message.Subject ?? "", message.Body?.Content ?? "", message.ToRecipients?.Select(recipient => recipient.EmailAddress?.Address ?? "").ToArray() ?? [], message.Attachments?.Select(attachment => attachment.Name ?? "").ToArray() ?? []));
        return Task.CompletedTask;
    }
    public Task SendEmailAsync(SendEmailRequest request)
    {
        outbox.Add(new(request.Subject, request.BodyHtml, request.ToRecipients.ToArray(), request.Attachments.Select(attachment => attachment.FileName).ToArray()));
        return Task.CompletedTask;
    }
}

// Employee lookups moved from Graph to the local directory data access.
// Adapt the same synthetic identities so UI records and hierarchy stay aligned.
internal sealed class TestDirectory(ICurrentUserAccessor me, TestOutbox outbox) : IADUsersDataAccess
{
    private readonly TestGraph _identities = new(me, outbox);

    public async Task<ADUser?> GetUserAsync(string userIdOrUpn) =>
        Convert(await _identities.GetUserAsync(userIdOrUpn));

    public async Task<ADUser?> GetManagerAsync(string userId) =>
        Convert(await _identities.GetManagerAsync(userId));

    public async Task<List<ADUser>> GetDirectReportsUsersAsync(string userId) =>
        (await _identities.GetDirectReportsUsersAsync(userId)).Select(user => Convert(user)!).ToList();

    public async Task<List<ADUser>> GetUsersBelowMyLevelAsync(string userId) =>
        (await _identities.GetUsersBelowMyLevelAsync()).Select(user => Convert(user)!).ToList();

    private static ADUser? Convert(User? user) => user is null ? null : new ADUser
    {
        ID = user.Id,
        DisplayName = user.DisplayName,
        Mail = user.Mail,
        UserPrincipalName = user.UserPrincipalName,
        Department = user.Department
    };
}

public sealed record TestMail(string Subject, string Body, string[] Recipients, string[] Attachments);
public sealed class TestOutbox
{
    private readonly ConcurrentQueue<TestMail> _messages = new();
    public void Add(TestMail message) => _messages.Enqueue(message);
    public TestMail[] Messages => _messages.ToArray();
}
