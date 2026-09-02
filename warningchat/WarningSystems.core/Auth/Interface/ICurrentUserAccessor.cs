using System.Security.Claims;

namespace WarningSystems.Core.Auth.Interface;

public interface ICurrentUserAccessor
{
    bool IsAuthenticated { get; }
    string? ObjectId { get; }
    string? UpnOrEmail { get; }
    string? DisplayName { get; }
    string AuditUser { get; }
    ClaimsPrincipal? Principal { get; }
}