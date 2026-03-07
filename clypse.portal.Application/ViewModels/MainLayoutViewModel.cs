using Blazing.Mvvm.ComponentModel;
using clypse.portal.Application.Services.Interfaces;
using clypse.portal.Models.Settings;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace clypse.portal.Application.ViewModels;

/// <summary>
/// ViewModel for the main layout, managing PWA updates and version display.
/// </summary>
public partial class MainLayoutViewModel : ViewModelBase
{
    private readonly IPwaUpdateService pwaUpdateService;
    private readonly AppSettings appSettings;
    private readonly ILogger<MainLayoutViewModel> logger;
    private CancellationTokenSource? updateLoopCts;

    private bool updateAvailable;
    private bool isUpdating;
    private bool showChangesDialog;

    /// <summary>
    /// Initializes a new instance of <see cref="MainLayoutViewModel"/>.
    /// </summary>
    public MainLayoutViewModel(
        IPwaUpdateService pwaUpdateService,
        AppSettings appSettings,
        ILogger<MainLayoutViewModel> logger)
    {
        this.pwaUpdateService = pwaUpdateService ?? throw new ArgumentNullException(nameof(pwaUpdateService));
        this.appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets a value indicating whether an update is available.
    /// </summary>
    public bool UpdateAvailable
    {
        get => updateAvailable;
        private set => SetProperty(ref updateAvailable, value);
    }

    /// <summary>
    /// Gets a value indicating whether an update is currently being installed.
    /// </summary>
    public bool IsUpdating
    {
        get => isUpdating;
        private set => SetProperty(ref isUpdating, value);
    }

    /// <summary>
    /// Gets a value indicating whether the changes dialog is shown.
    /// </summary>
    public bool ShowChangesDialog
    {
        get => showChangesDialog;
        private set => SetProperty(ref showChangesDialog, value);
    }

    /// <summary>
    /// Gets the current application version.
    /// </summary>
    public string AvailableVersion => appSettings.Version;

    /// <summary>
    /// Gets the application settings.
    /// </summary>
    public AppSettings AppSettings => appSettings;

    /// <inheritdoc/>
    public override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await SetupPwaUpdateServiceAsync();
            updateLoopCts = new CancellationTokenSource();
            _ = RunUpdateLoopAsync(updateLoopCts.Token);
        }
    }

    /// <summary>
    /// Handles a click on the version number to show the changes dialog.
    /// </summary>
    [RelayCommand]
    public async Task HandleVersionClickAsync()
    {
        ShowChangesDialog = true;

        if (!UpdateAvailable)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await pwaUpdateService.CheckForUpdateAsync();
                    await Task.Delay(1500);
                    UpdateAvailable = await pwaUpdateService.IsUpdateAvailableAsync();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error checking for updates in background");
                }
            });
        }
    }

    /// <summary>
    /// Handles closing the changes dialog.
    /// </summary>
    [RelayCommand]
    public void HandleCloseChangesDialog()
    {
        ShowChangesDialog = false;
    }

    /// <summary>
    /// Handles installing an available update.
    /// </summary>
    [RelayCommand]
    public async Task HandleInstallUpdateAsync()
    {
        if (IsUpdating)
        {
            return;
        }

        try
        {
            IsUpdating = true;
            logger.LogInformation("Installing available update");

            var installResult = await pwaUpdateService.InstallUpdateAsync();
            if (!installResult)
            {
                logger.LogInformation("Install failed, trying force update");
                await pwaUpdateService.ForceUpdateAsync();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error installing update");
        }
        finally
        {
            IsUpdating = false;
        }
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            updateLoopCts?.Cancel();
            updateLoopCts?.Dispose();
            updateLoopCts = null;

            try
            {
                pwaUpdateService.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error disposing MainLayoutViewModel");
            }
        }

        base.Dispose(disposing);
    }

    private async Task SetupPwaUpdateServiceAsync()
    {
        try
        {
            await pwaUpdateService.SetupUpdateCallbacksAsync();
            logger.LogInformation("PWA update service initialized");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error setting up PWA update service");
        }
    }

    private async Task RunUpdateLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(2000, cancellationToken);
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var wasUpdateAvailable = UpdateAvailable;
                    UpdateAvailable = await pwaUpdateService.IsUpdateAvailableAsync();

                    await Task.Delay(30000, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error checking for updates");
                    await Task.Delay(60000, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on disposal
        }
    }
}
