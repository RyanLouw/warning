using Microsoft.Identity.Web;
using Microsoft.Kiota.Abstractions.Authentication;

namespace WarningSystems.Core.DataAccess.GraphDataAccess.Context;

public sealed class TokenAcquisitionTokenProvider : IAccessTokenProvider
{
    private readonly ITokenAcquisition _tokenAcquisition;
    private readonly string[] _scopes;

    public TokenAcquisitionTokenProvider(ITokenAcquisition tokenAcquisition, string[] scopes)
    {
        _tokenAcquisition = tokenAcquisition;
        _scopes = scopes;
    }

    public AllowedHostsValidator AllowedHostsValidator { get; } =
        new AllowedHostsValidator(new[] { "graph.microsoft.com" });

    public Task<string> GetAuthorizationTokenAsync(
        Uri uri,
        Dictionary<string, object>? additionalAuthenticationContext = null,
        CancellationToken cancellationToken = default)
    {
        return _tokenAcquisition.GetAccessTokenForUserAsync(_scopes);
    }
}