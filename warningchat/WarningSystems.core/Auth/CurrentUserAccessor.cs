using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using WarningSystems.Core.Auth.Interface;

namespace WarningSystems.Core.Auth;

public class CurrentUserAccessor : ICurrentUserAccessor
{
    private readonly IHttpContextAccessor _http;

    public CurrentUserAccessor(IHttpContextAccessor http)
    {
        _http = http;
    }

    public ClaimsPrincipal? Principal => _http.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public string? UpnOrEmail =>
        Principal?.FindFirst("preferred_username")?.Value
        ?? Principal?.FindFirst(ClaimTypes.Upn)?.Value
        ?? Principal?.FindFirst(ClaimTypes.Email)?.Value
        ?? Principal?.Identity?.Name;

    public string? DisplayName =>
        Principal?.FindFirst("name")?.Value
        ?? Principal?.FindFirst(ClaimTypes.Name)?.Value
        ?? Principal?.Identity?.Name
        ?? UpnOrEmail;

    public string? ObjectId =>
        Principal?.FindFirst("oid")?.Value
        ?? Principal?.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
        ?? Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? Principal?.FindFirst("sub")?.Value;

    public string AuditUser => UpnOrEmail ?? ObjectId ?? "unknown";

    public string RequireAuditUser()
    {
        if (!IsAuthenticated)
            throw new InvalidOperationException("User not authenticated.");

        return AuditUser;
    }
}