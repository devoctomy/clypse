namespace clypse.portal.setup.Services.Build;

/// <summary>
/// Updates service worker asset manifest hashes after modifying published files.
/// </summary>
public interface IServiceWorkerAssetHashUpdaterService
{
    /// <summary>
    /// Updates the hash for a specific asset in the service worker manifest.
    /// </summary>
    /// <param name="publishDirectory">The directory containing the published Blazor app.</param>
    /// <param name="assetPath">The relative path of the asset to update (e.g., "appsettings.json").</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>True if the asset hash was successfully updated, false otherwise.</returns>
    Task<bool> UpdateAssetAsync(
        string publishDirectory,
        string assetPath,
        CancellationToken cancellationToken = default);
}
