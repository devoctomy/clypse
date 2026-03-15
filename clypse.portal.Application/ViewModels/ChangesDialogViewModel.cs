using System.Text.Json;
using Blazing.Mvvm.ComponentModel;
using clypse.portal.Application.Helpers;
using clypse.portal.Models.Changes;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace clypse.portal.Application.ViewModels;

/// <summary>
/// ViewModel for the changes/changelog dialog.
/// </summary>
public partial class ChangesDialogViewModel : ViewModelBase
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new ()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient httpClient;
    private readonly ILogger<ChangesDialogViewModel> logger;

    private bool isLoading;
    private bool isUpdating;
    private string? errorMessage;
    private ChangeLog? changeLog;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChangesDialogViewModel"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to fetch the changelog.</param>
    /// <param name="logger">The logger instance.</param>
    public ChangesDialogViewModel(HttpClient httpClient, ILogger<ChangesDialogViewModel> logger)
    {
        this.httpClient = ValidationHelpers.VerifiedAssignent(httpClient);
        this.logger = ValidationHelpers.VerifiedAssignent(logger);
    }

    /// <summary>Gets a value indicating whether the changelog is loading.</summary>
    public bool IsLoading { get => isLoading; private set => SetProperty(ref isLoading, value); }

    /// <summary>Gets a value indicating whether an update is in progress.</summary>
    public bool IsUpdating { get => isUpdating; private set => SetProperty(ref isUpdating, value); }

    /// <summary>Gets the error message if loading failed.</summary>
    public string? ErrorMessage { get => errorMessage; private set => SetProperty(ref errorMessage, value); }

    /// <summary>Gets the loaded changelog.</summary>
    public ChangeLog? ChangeLog { get => changeLog; private set => SetProperty(ref changeLog, value); }

    /// <summary>Gets or sets the callback invoked when the dialog should close.</summary>
    public Func<Task>? OnCloseCallback { get; set; }

    /// <summary>Gets or sets the callback invoked when an update is requested.</summary>
    public Func<Task>? OnUpdateCallback { get; set; }

    /// <summary>
    /// Loads the changelog if not already loaded.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task EnsureChangelogLoadedAsync()
    {
        if (ChangeLog == null)
        {
            await LoadChangelogAsync();
        }
    }

    /// <summary>Closes the dialog.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RelayCommand]
    public async Task HandleCloseAsync()
    {
        if (OnCloseCallback != null)
        {
            await OnCloseCallback();
        }
    }

    /// <summary>Installs the available update.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RelayCommand]
    public async Task HandleUpdateAsync()
    {
        IsUpdating = true;
        try
        {
            if (OnUpdateCallback != null)
            {
                await OnUpdateCallback();
            }
        }
        finally
        {
            IsUpdating = false;
        }
    }

    private async Task LoadChangelogAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var json = await httpClient.GetStringAsync("changes.json");
            ChangeLog = JsonSerializer.Deserialize<ChangeLog>(json, JsonSerializerOptions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error loading changelog");
            ErrorMessage = "Failed to load version history. Please try again later.";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
