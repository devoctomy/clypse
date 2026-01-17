namespace clypse.portal.setup.Services.Security;

/// <summary>
/// Provides access to security token metadata.
/// </summary>
public interface ISecurityTokenService
{
    /// <summary>
    /// Retrieves the AWS account identifier for the current credentials.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The AWS account identifier.</returns>
    public Task<string> GetAccountIdAsync(CancellationToken cancellationToken = default);
}
