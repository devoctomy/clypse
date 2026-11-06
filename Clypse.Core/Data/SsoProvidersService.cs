namespace Clypse.Core.Data;

/// <summary>
/// Service for retrieving SSO providers.
/// </summary>
/// <param name="embeddedResourceLoaderService">IEmbeddedResourceLoaderService instance.</param>
public class SsoProvidersService(
    IEmbeddedResourceLoaderService embeddedResourceLoaderService)
    : ISsoProvidersService
{
    /// <summary>
    /// Gets a list of SSO providers.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A list of SSO provider names.</returns>
    public async Task<List<string>> GetSsoProvidersAsync(CancellationToken cancellationToken)
    {
        var ssoProviders = await embeddedResourceLoaderService.LoadHashSetAsync(
            ResourceKeys.SsoProvidersResourceKey,
            typeof(SsoProvidersService).Assembly,
            cancellationToken);
        return [.. ssoProviders];
    }
}
