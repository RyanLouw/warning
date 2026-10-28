using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace WarningSystems.Testing;

// Excluded from normal builds by WarningSystems.csproj.
public sealed class TestAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration configuration) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "AspireTest";
    public const string KeyHeader = "X-Warning-Test-Key";
    public const string UserHeader = "X-Warning-Test-User";
    private static readonly HashSet<string> Users = ["User", "Legal", "Admin", "Chairperson"];

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var expected = configuration["WarningSystemTesting:Key"] ?? "";
        var supplied = Request.Headers[KeyHeader].ToString();
        if (expected.Length < 32 || !CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(supplied)))
            return Task.FromResult(AuthenticateResult.NoResult());
        var user = Request.Headers[UserHeader].ToString();
        if (!Users.Contains(user)) return Task.FromResult(AuthenticateResult.Fail("Unknown test user."));
        var id = $"test-{user.ToLowerInvariant()}";
        var claims = new[] {
            new Claim("oid", id), new Claim(ClaimTypes.NameIdentifier, id),
            new Claim("name", $"Test {user}"), new Claim(ClaimTypes.Name, $"Test {user}"),
            new Claim("preferred_username", $"{user.ToLowerInvariant()}@example.test"),
            new Claim(ClaimTypes.Role, user)
        };
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(
            new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName)), SchemeName)));
    }
}
