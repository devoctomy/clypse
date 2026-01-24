
using Amazon.SecurityToken;

namespace clypse.portal.setup.Services.Security;

/// <inheritdoc cref="ISecurityTokenService" />
public class SecurityTokenService(IAmazonSecurityTokenService securityTokenService) : ISecurityTokenService
{
    /// <inheritdoc />
    public async Task<string> GetAccountIdAsync(CancellationToken cancellationToken = default)
    {
        var getCallerIdentityRequest = new Amazon.SecurityToken.Model.GetCallerIdentityRequest();
        var callerIdentity = await securityTokenService.GetCallerIdentityAsync(getCallerIdentityRequest, cancellationToken);
        return callerIdentity.Account;
    }
}
