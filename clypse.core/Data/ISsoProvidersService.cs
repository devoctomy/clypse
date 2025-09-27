namespace clypse.core.Data;

/// <summary>
/// Service for retrieving SSO providers.
/// </summary>
public interface ISsoProvidersService
{
    /// <summary>
    /// Gets a list of SSO providers.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A list of SSO provider names.</returns>
    public Task<List<string>> GetSsoProvidersAsync(CancellationToken cancellationToken);
}
