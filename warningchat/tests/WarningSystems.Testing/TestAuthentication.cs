using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WarningSystems.Core.Services.Interface;

namespace WarningSystems.Testing;

// This assembly is referenced only by tests. There is no test-login endpoint in the application.
public sealed class TestAuthentication(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "PipelineTest";
    public const string RoleHeader = "X-Test-Roles";
    private static readonly HashSet<string> AllowedRoles = ["User", "Legal", "Admin", "Chairperson"];

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers[RoleHeader].ToString();
        if (header == "Anonymous") return Task.FromResult(AuthenticateResult.NoResult());
        var roles = (string.IsNullOrEmpty(header) ? "User,Legal" : header).Split(',');
        if (roles.Any(role => !AllowedRoles.Contains(role)))
            return Task.FromResult(AuthenticateResult.Fail("Unknown synthetic test role."));
        var claims = new List<Claim> {
            new("oid", SeedData.ManagerId), new(ClaimTypes.NameIdentifier, SeedData.ManagerId),
            new(ClaimTypes.Name, "Test Manager"), new("name", "Test Manager"),
            new("preferred_username", "manager@example.test")
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(
            new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName)), SchemeName)));
    }
}

public sealed class TestRoleService(IHttpContextAccessor accessor) : IUserRoleService
{
    public Task<IReadOnlyList<string>> GetCurrentUserRolesAsync() =>
        Task.FromResult<IReadOnlyList<string>>(accessor.HttpContext?.User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray() ?? []);

    public async Task<bool> CurrentUserIsInRoleAsync(string role) =>
        (await GetCurrentUserRolesAsync()).Contains(role, StringComparer.OrdinalIgnoreCase);
}
