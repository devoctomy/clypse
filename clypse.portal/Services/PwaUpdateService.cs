using Microsoft.JSInterop;

namespace clypse.portal.Services;

/// <summary>
/// Implementation of PWA update service using JavaScript interop.
/// </summary>
public class PwaUpdateService : IPwaUpdateService, IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<PwaUpdateService> _logger;
    private DotNetObjectReference<PwaUpdateService>? _objectReference;
    private Func<Task>? _onUpdateAvailable;
    private Func<Task>? _onUpdateInstalled;
    private Func<string, Task>? _onUpdateError;

    public PwaUpdateService(IJSRuntime jsRuntime, ILogger<PwaUpdateService> logger)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> IsUpdateAvailableAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<bool>("PWAUpdateService.isUpdateAvailable");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if PWA update is available");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> CheckForUpdateAsync()
    {
        try
        {
            _logger.LogInformation("Checking for PWA updates");
            return await _jsRuntime.InvokeAsync<bool>("PWAUpdateService.checkForUpdate");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for PWA updates");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> InstallUpdateAsync()
    {
        try
        {
            _logger.LogInformation("Installing PWA update");
            return await _jsRuntime.InvokeAsync<bool>("PWAUpdateService.installUpdate");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error installing PWA update");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> ForceUpdateAsync()
    {
        try
        {
            _logger.LogInformation("Force updating PWA");
            return await _jsRuntime.InvokeAsync<bool>("PWAUpdateService.forceUpdate");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error force updating PWA");
            return false;
        }
    }

    /// <inheritdoc />
    public Task SetupUpdateCallbacksAsync(Func<Task>? onUpdateAvailable = null, Func<Task>? onUpdateInstalled = null, Func<string, Task>? onUpdateError = null)
    {
        try
        {
            _onUpdateAvailable = onUpdateAvailable;
            _onUpdateInstalled = onUpdateInstalled;
            _onUpdateError = onUpdateError;

            // Simple approach: just let the PWA service handle updates internally
            // We'll poll for updates instead of using callbacks
            _logger.LogInformation("PWA update service initialized");
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting up PWA update callbacks");
            return Task.CompletedTask;
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        try
        {
            _objectReference?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing PWA update service");
        }
        
        return ValueTask.CompletedTask;
    }
}