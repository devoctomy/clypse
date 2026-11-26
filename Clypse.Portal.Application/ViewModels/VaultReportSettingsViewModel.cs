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
/// ViewModel for the vault report section of Settings: after how many months without being updated a password is
/// reported as old. Kept on the device.
/// </summary>
public partial class VaultReportSettingsViewModel : ViewModelBase
{
    /// <summary>
    /// The ages offered, in months.
    /// </summary>
    public static readonly IReadOnlyList<int> MonthsOptions = [1, 3, 6, 12, 24];

    private readonly IVaultReportPreferencesService preferencesService;
    private readonly IToastService toasts;
    private readonly ITextPlurals plurals;
    private readonly IStringLocalizer<ClypseText> text;

    private VaultReportPreferences preferences = new VaultReportPreferences();
    private bool isLoaded;

    /// <summary>
    /// Initializes a new instance of the <see cref="VaultReportSettingsViewModel"/> class.
    /// </summary>
    /// <param name="preferencesService">Keeps the preferences on the device.</param>
    /// <param name="toasts">Says they were saved.</param>
    /// <param name="plurals">Words the ages.</param>
    /// <param name="text">Clypse's text.</param>
    public VaultReportSettingsViewModel(
        IVaultReportPreferencesService preferencesService,
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
    /// Gets or sets after how many months a password is old.
    /// </summary>
    public int OldPasswordMonths
    {
        get => preferences.OldPasswordMonths;
        set
        {
            preferences.OldPasswordMonths = Math.Max(VaultReportPreferences.MinimumMonths, value);
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Gets the ages to choose from: <see cref="MonthsOptions"/>, and the one chosen if it is not among them.
    /// </summary>
    public IReadOnlyList<int> Options => MonthsOptions.Contains(OldPasswordMonths)
        ? MonthsOptions
        : [.. MonthsOptions.Append(OldPasswordMonths).Order()];

    /// <inheritdoc/>
    public override async Task OnInitializedAsync()
    {
        preferences = await preferencesService.GetAsync();
        IsLoaded = true;
        OnPropertyChanged(nameof(OldPasswordMonths));
        OnPropertyChanged(nameof(Options));
    }

    /// <summary>
    /// Words an age, e.g. "3 months".
    /// </summary>
    /// <param name="months">The age, in months.</param>
    /// <returns>The text.</returns>
    public string GetMonthsText(int months) => plurals.Format(text, "VaultReportSettings.Months", months);

    /// <summary>
    /// Keeps the preferences on the device.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RelayCommand]
    public async Task SaveAsync()
    {
        await preferencesService.SaveAsync(preferences);
        toasts.Show(text["VaultReportSettings.Saved"], ToastTone.Success);
    }
}
