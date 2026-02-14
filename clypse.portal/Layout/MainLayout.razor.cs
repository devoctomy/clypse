using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using clypse.portal.Models;
using clypse.portal.Services;
using clypse.portal.Models.Settings;

namespace clypse.portal.Layout;

public partial class MainLayout : LayoutComponentBase, IAsyncDisposable
{
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private AppSettings AppSettings { get; set; } = default!;
    [Inject] private IPwaUpdateService PwaUpdateService { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] private ILogger<MainLayout> Logger { get; set; } = default!;

    private bool updateAvailable;
    private bool isUpdating;
    private bool showChangesDialog;
    private string availableVersion => AppSettings.Version;
    
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await SetupPwaUpdateService();
            
            // Start periodic update checking
            _ = Task.Run(async () =>
            {
                await Task.Delay(2000); // Wait a bit for everything to initialize
                await CheckForUpdatesLoop();
            });
        }
    }

    private async Task SetupPwaUpdateService()
    {
        try
        {
            await PwaUpdateService.SetupUpdateCallbacksAsync();
            Logger.LogInformation("PWA update service initialized");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error setting up PWA update service");
        }
    }

    private async Task CheckForUpdatesLoop()
    {
        while (true)
        {
            try
            {
                var wasUpdateAvailable = updateAvailable;
                updateAvailable = await PwaUpdateService.IsUpdateAvailableAsync();
                
                if (updateAvailable != wasUpdateAvailable)
                {
                    await InvokeAsync(StateHasChanged);
                }
                
                // Check every 30 seconds
                await Task.Delay(30000);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error checking for updates");
                await Task.Delay(60000); // Wait longer on error
            }
        }
    }

    private async Task HandleVersionClick()
    {
        // Simply show the changes dialog
        showChangesDialog = true;
        StateHasChanged();
        
        // If no update is currently available, check for one in the background
        if (!updateAvailable)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    Logger.LogInformation("Checking for new updates in background");
                    await PwaUpdateService.CheckForUpdateAsync();
                    
                    // Wait a moment and check if update became available
                    await Task.Delay(1500);
                    updateAvailable = await PwaUpdateService.IsUpdateAvailableAsync();
                    
                    if (updateAvailable)
                    {
                        await InvokeAsync(StateHasChanged);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error checking for updates in background");
                }
            });
        }
    }

    private void HandleCloseChangesDialog()
    {
        showChangesDialog = false;
        StateHasChanged();
    }

    private async Task HandleInstallUpdate()
    {
        if (isUpdating) return;

        try
        {
            isUpdating = true;
            StateHasChanged();

            Logger.LogInformation("Installing available update");
            var installResult = await PwaUpdateService.InstallUpdateAsync();
            
            if (!installResult)
            {
                // If install failed, try force update
                Logger.LogInformation("Install failed, trying force update");
                await PwaUpdateService.ForceUpdateAsync();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error installing update");
            await JSRuntime.InvokeVoidAsync("console.error", $"Update error: {ex.Message}");
        }
        finally
        {
            isUpdating = false;
            StateHasChanged();
        }
    }
    
    private string GetCurrentPageName()
    {
        var uri = Navigation.ToAbsoluteUri(Navigation.Uri);
        var path = uri.AbsolutePath;
        
        return path switch
        {
            "/" => "Login Page",
            "/test" => "Test Page",
            _ => "Unknown Page"
        };
    }

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        try
        {
            await PwaUpdateService.DisposeAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error disposing MainLayout");
        }
    }
}
