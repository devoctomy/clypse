using Blazing.Mvvm.ComponentModel;
using Clypse.Portal.Application.Services.Interfaces;
using Clypse.Portal.Application.Text;
using Clypse.Portal.Models.Settings;
using CommunityToolkit.Mvvm.Input;
using Devoctomy.Blazwiz.Core.Text.Interfaces;
using Devoctomy.Blazwiz.UI.Models;
using Devoctomy.Blazwiz.UI.Services.Interfaces;
using Microsoft.Extensions.Localization;

namespace Clypse.Portal.Application.ViewModels;

/// <summary>
/// ViewModel for the clipboard section of Settings: whether a copied password or other secret value is cleared from
/// the clipboard again, and after how long. Kept on the device.
/// </summary>
public partial class ClipboardSettingsViewModel : ViewModelBase
{
    /// <summary>
    /// The times offered, in seconds.
    /// </summary>
    public static readonly IReadOnlyList<int> SecondsOptions = [10, 15, 30, 60, 120];

    private readonly IClipboardPreferencesService preferencesService;
    private readonly IToastService toasts;
    private readonly ITextPlurals plurals;
    private readonly IStringLocalizer<ClypseText> text;

    private ClipboardPreferences preferences = new ClipboardPreferences();
    private bool isLoaded;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClipboardSettingsViewModel"/> class.
    /// </summary>
    /// <param name="preferencesService">Keeps the preferences on the device.</param>
    /// <param name="toasts">Says they were saved.</param>
    /// <param name="plurals">Words the times.</param>
    /// <param name="text">Clypse's text.</param>
    public ClipboardSettingsViewModel(
        IClipboardPreferencesService preferencesService,
        IToastService toasts,
        ITextPlurals plurals,
        IStringLocalizer<ClypseText> text)
    {
        this.preferencesService = preferencesService ?? throw new ArgumentNullException(nameof(preferencesService));
        this.toasts = toasts ?? throw new ArgumentNullException(nameof(toasts));
        this.plurals = plurals ?? throw new ArgumentNullException(nameof(plurals));
        this.text = text ?? throw new ArgumentNullException(nameof(text));
    }

    /// <summary>
    /// Gets a value indicating whether the preferences have been read.
    /// </summary>
    public bool IsLoaded
    {
        get => isLoaded;
        private set => SetProperty(ref isLoaded, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether a copied secret value is cleared from the clipboard.
    /// </summary>
    public bool ClearSecrets
    {
        get => preferences.ClearSecrets;
        set
        {
            preferences.ClearSecrets = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Gets or sets how long, in seconds, a copied secret value stays on the clipboard.
    /// </summary>
    public int ClearAfterSeconds
    {
        get => preferences.ClearAfterSeconds;
        set
        {
            preferences.ClearAfterSeconds = Math.Max(ClipboardPreferences.MinimumSeconds, value);
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Gets the times to choose from: <see cref="SecondsOptions"/>, and the one chosen if it is not among them.
    /// </summary>
    public IReadOnlyList<int> Options => SecondsOptions.Contains(ClearAfterSeconds)
        ? SecondsOptions
        : [.. SecondsOptions.Append(ClearAfterSeconds).Order()];

    /// <inheritdoc/>
    public override async Task OnInitializedAsync()
    {
        preferences = await preferencesService.GetAsync();
        IsLoaded = true;
        OnPropertyChanged(nameof(ClearSecrets));
        OnPropertyChanged(nameof(ClearAfterSeconds));
        OnPropertyChanged(nameof(Options));
    }

    /// <summary>
    /// Words a time, e.g. "30 seconds".
    /// </summary>
    /// <param name="seconds">The time, in seconds.</param>
    /// <returns>The text.</returns>
    public string GetSecondsText(int seconds) => plurals.Format(text, "ClipboardSettings.Seconds", seconds);

    /// <summary>
    /// Keeps the preferences on the device.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RelayCommand]
    public async Task SaveAsync()
    {
        await preferencesService.SaveAsync(preferences);
        toasts.Show(text["ClipboardSettings.Saved"], ToastTone.Success);
    }
}
