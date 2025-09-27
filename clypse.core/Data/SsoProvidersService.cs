namespace clypse.core.Data;

/// <summary>
/// Service for retrieving SSO providers.
/// </summary>
/// <param name="embeddedResorceLoaderService">IEmbeddedResorceLoaderService instance.</param>
public class SsoProvidersService(
    IEmbeddedResorceLoaderService embeddedResorceLoaderService)
    : ISsoProvidersService
{
    /// <summary>
    /// Gets a list of SSO providers.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A list of SSO provider names.</returns>
    public async Task<List<string>> GetSsoProvidersAsync(CancellationToken cancellationToken)
    {
        var ssoProviders = await embeddedResorceLoaderService.LoadHashSetAsync(
            ResourceKeys.SsoProvidersResourceKey,
            typeof(SsoProvidersService).Assembly,
            cancellationToken);
        return [.. ssoProviders];
    }
}
