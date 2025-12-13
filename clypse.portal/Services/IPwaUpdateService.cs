namespace clypse.portal.Services;

/// <summary>
/// Service for handling PWA updates and version management.
/// </summary>
public interface IPwaUpdateService : IAsyncDisposable
{
    /// <summary>
    /// Check if a PWA update is available.
    /// </summary>
    /// <returns>True if an update is available.</returns>
    Task<bool> IsUpdateAvailableAsync();

    /// <summary>
    /// Manually check for PWA updates.
    /// </summary>
    /// <returns>True if update check was successful.</returns>
    Task<bool> CheckForUpdateAsync();

    /// <summary>
    /// Install a waiting PWA update.
    /// </summary>
    /// <returns>True if update installation was initiated.</returns>
    Task<bool> InstallUpdateAsync();

    /// <summary>
    /// Force check for updates and install if available.
    /// </summary>
    /// <returns>True if operation was successful.</returns>
    Task<bool> ForceUpdateAsync();

    /// <summary>
    /// Set up callback handlers for update events.
    /// </summary>
    /// <param name="onUpdateAvailable">Callback when update becomes available.</param>
    /// <param name="onUpdateInstalled">Callback when update is installed.</param>
    /// <param name="onUpdateError">Callback when update error occurs.</param>
    Task SetupUpdateCallbacksAsync(Func<Task>? onUpdateAvailable = null, Func<Task>? onUpdateInstalled = null, Func<string, Task>? onUpdateError = null);
}