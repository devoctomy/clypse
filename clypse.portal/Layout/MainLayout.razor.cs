using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using clypse.portal.Models;
using clypse.portal.Services;

namespace clypse.portal.Layout;

public partial class MainLayout : LayoutComponentBase, IAsyncDisposable
{
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private AppSettings AppSettings { get; set; } = default!;
    [Inject] private IPwaUpdateService PwaUpdateService { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] private ILogger<MainLayout> Logger { get; set; } = default!;

    private bool updateAvailable = false;
    private bool isUpdating = false;
    
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
        if (isUpdating) return;

        try
        {
            isUpdating = true;
            StateHasChanged();

            Logger.LogInformation("Version clicked - checking for updates");

            if (updateAvailable)
            {
                // Install the waiting update
                Logger.LogInformation("Installing available update");
                var installResult = await PwaUpdateService.InstallUpdateAsync();
                
                if (!installResult)
                {
                    // If install failed, try force update
                    Logger.LogInformation("Install failed, trying force update");
                    await PwaUpdateService.ForceUpdateAsync();
                }
            }
            else
            {
                // Check for new updates
                Logger.LogInformation("Checking for new updates");
                var checkResult = await PwaUpdateService.CheckForUpdateAsync();
                
                if (!checkResult)
                {
                    // Show no updates available message
                    await JSRuntime.InvokeVoidAsync("console.log", "No updates available");
                }
                
                // Wait a moment and check if update became available
                await Task.Delay(1500);
                updateAvailable = await PwaUpdateService.IsUpdateAvailableAsync();
                
                if (updateAvailable)
                {
                    // New update found, install it
                    Logger.LogInformation("New update found, installing");
                    await PwaUpdateService.InstallUpdateAsync();
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error handling version click");
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
