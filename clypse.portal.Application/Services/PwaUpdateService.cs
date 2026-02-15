using clypse.portal.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace clypse.portal.Application.Services;

/// <inheritdoc/>
public class PwaUpdateService(IJSRuntime jsRuntime, ILogger<PwaUpdateService> logger)
    : IPwaUpdateService, IAsyncDisposable
{
    private readonly IJSRuntime jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
    private readonly ILogger<PwaUpdateService> logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private Func<Task>? onUpdateAvailable;
    private Func<Task>? onUpdateInstalled;
    private Func<string, Task>? onUpdateError;

    /// <inheritdoc />
    public async Task<bool> IsUpdateAvailableAsync()
    {
        try
        {
            return await this.jsRuntime.InvokeAsync<bool>("PWAUpdateService.isUpdateAvailable");
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error checking if PWA update is available");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> CheckForUpdateAsync()
    {
        try
        {
            this.logger.LogInformation("Checking for PWA updates");
            return await this.jsRuntime.InvokeAsync<bool>("PWAUpdateService.checkForUpdate");
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error checking for PWA updates");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> InstallUpdateAsync()
    {
        try
        {
            this.logger.LogInformation("Installing PWA update");
            return await this.jsRuntime.InvokeAsync<bool>("PWAUpdateService.installUpdate");
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error installing PWA update");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> ForceUpdateAsync()
    {
        try
        {
            this.logger.LogInformation("Force updating PWA");
            return await this.jsRuntime.InvokeAsync<bool>("PWAUpdateService.forceUpdate");
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error force updating PWA");
            return false;
        }
    }

    /// <inheritdoc />
    public Task SetupUpdateCallbacksAsync(Func<Task>? onUpdateAvailable = null, Func<Task>? onUpdateInstalled = null, Func<string, Task>? onUpdateError = null)
    {
        try
        {
            this.onUpdateAvailable = onUpdateAvailable;
            this.onUpdateInstalled = onUpdateInstalled;
            this.onUpdateError = onUpdateError;

            // Simple approach: just let the PWA service handle updates internally
            // We'll poll for updates instead of using callbacks
            this.logger.LogInformation("PWA update service initialized");
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error setting up PWA update callbacks");
            return Task.CompletedTask;
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        // Nothing to dispose
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}