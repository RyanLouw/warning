using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace WarningSystems.Core.Services;

public sealed class AppRoleClaimsTransformation : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal?.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
            return Task.FromResult(principal);

        var existingRoles = identity.FindAll(ClaimTypes.Role)
            .Select(c => c.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var rawRoles = existingRoles.ToList();

        foreach (var role in rawRoles)
        {
            if (role.Contains("Admin", StringComparison.OrdinalIgnoreCase) &&
                !existingRoles.Contains("Admin"))
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, "Admin"));
                existingRoles.Add("Admin");
            }

            if (role.Contains("Legal", StringComparison.OrdinalIgnoreCase) &&
                !existingRoles.Contains("Legal"))
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, "Legal"));
                existingRoles.Add("Legal");
            }

            if (role.Contains("Chairperson", StringComparison.OrdinalIgnoreCase) &&
                !existingRoles.Contains("Chairperson"))
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, "Chairperson"));
                existingRoles.Add("Chairperson");
            }

            if (role.Contains("User", StringComparison.OrdinalIgnoreCase) &&
                !existingRoles.Contains("User"))
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, "User"));
                existingRoles.Add("User");
            }
        }

        return Task.FromResult(principal);
    }
}