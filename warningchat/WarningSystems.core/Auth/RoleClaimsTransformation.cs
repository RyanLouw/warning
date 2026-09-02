using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using WarningSystems.Core.Services.Interface;

namespace WarningSystems.Core.Auth
{
    public class RoleClaimsTransformation : IClaimsTransformation
    {
        private readonly IUserRoleService _roles;

        public RoleClaimsTransformation(IUserRoleService roles)
        {
            _roles = roles;
        }

        public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            if (principal.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
            {
                return principal;
            }

            var existing = new HashSet<string>(
                identity.FindAll(ClaimTypes.Role).Select(c => c.Value),
                StringComparer.OrdinalIgnoreCase);

            var roles = await _roles.GetCurrentUserRolesAsync();

            foreach (var role in roles)
            {
                if (existing.Add(role))
                {
                    identity.AddClaim(new Claim(ClaimTypes.Role, role));
                }
            }

            return principal;
        }
    }
}