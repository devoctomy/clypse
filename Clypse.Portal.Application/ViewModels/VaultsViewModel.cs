using Blazing.Mvvm.ComponentModel;
using Clypse.Portal.Application.Services.Interfaces;
using Clypse.Portal.Application.Text;
using Clypse.Portal.Models.Vaults;
using CommunityToolkit.Mvvm.Input;
using Devoctomy.Blazwiz.Browser.Services.Interfaces;
using Devoctomy.Blazwiz.Encryption.Interfaces;
using Devoctomy.Blazwiz.Encryption.Models;
using Devoctomy.Blazwiz.Sync.Encryption.Interfaces;
using Devoctomy.Blazwiz.Sync.Interfaces;
using Devoctomy.Blazwiz.UI.Text.Interfaces;
using Microsoft.Extensions.Localization;

namespace Clypse.Portal.Application.ViewModels;

/// <summary>
/// ViewModel for the vaults page: the user's vaults, each with its own passphrase, and a dialog to create one. Opening
/// a vault shows its secrets. Passphrases are cleared from the view model once used.
/// </summary>
public partial class VaultsViewModel : ViewModelBase
{
    /// <summary>
    /// The vaults page's address.
    /// </summary>
    public const string Url = "vaults";

    private readonly IVaultService vaultService;
    private readonly ISyncProtectedProfileService profiles;
    private readonly IE2eProtectedProfileManager manager;
    private readonly INavigationService navigationService;
    private readonly ISyncScheduler scheduler;
    private readonly IErrorText errorText;
    private readonly IStringLocalizer<ClypseText> text;

    private IReadOnlyList<Vault> vaults = [];
    private bool isLoading = true;
    private bool isCreateDialogOpen;
    private string name = string.Empty;
    private string description = string.Empty;
    private string passphrase = string.Empty;
    private string confirmPassphrase = string.Empty;
    private bool acknowledgedNoRecovery;
    private bool isWorking;
    private string? dialogError;

    /// <summary>
    /// Initializes a new instance of the <see cref="VaultsViewModel"/> class.
    /// </summary>
    /// <param name="vaultService">The user's vaults.</param>
    /// <param name="profiles">Says when vaults are loaded, created or deleted.</param>
    /// <param name="manager">Says when a vault is unlocked or locked.</param>
    /// <param name="navigationService">Opens a vault.</param>
    /// <param name="errorText">Describes errors.</param>
    /// <param name="text">Clypse's text.</param>
    /// <param name="scheduler">Syncs a vault before it is unlocked with biometrics.</param>
    public VaultsViewModel(
        IVaultService vaultService,
        ISyncProtectedProfileService profiles,
        IE2eProtectedProfileManager manager,
        INavigationService navigationService,
        IErrorText errorText,
        IStringLocalizer<ClypseText> text,
        ISyncScheduler scheduler)
    {
        this.scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
        this.vaultService = vaultService ?? throw new ArgumentNullException(nameof(vaultService));
        this.profiles = profiles ?? throw new ArgumentNullException(nameof(profiles));
        this.manager = manager ?? throw new ArgumentNullException(nameof(manager));
        this.navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        this.errorText = errorText ?? throw new ArgumentNullException(nameof(errorText));
        this.text = text ?? throw new ArgumentNullException(nameof(text));
        this.profiles.ProfilesChanged += OnProfilesChanged;
        this.manager.LockStateChanged += OnLockStateChanged;
    }

    /// <summary>
    /// Gets the user's vaults, by name.
    /// </summary>
    public IReadOnlyList<Vault> Vaults
    {
        get => vaults;
        private set => SetProperty(ref vaults, value);
    }

    /// <summary>
    /// Gets a value indicating whether the vaults are being loaded for the first time.
    /// </summary>
    public bool IsLoading
    {
        get => isLoading;
        private set => SetProperty(ref isLoading, value);
    }

    /// <summary>
    /// Gets a value indicating whether the new vault dialog is open.
    /// </summary>
    public bool IsCreateDialogOpen
    {
        get => isCreateDialogOpen;
        private set => SetProperty(ref isCreateDialogOpen, value);
    }

    /// <summary>
    /// Gets or sets the new vault's name.
    /// </summary>
    public string Name
    {
        get => name;
        set => SetProperty(ref name, value ?? string.Empty);
    }

    /// <summary>
    /// Gets or sets the new vault's description.
    /// </summary>
    public string Description
    {
        get => description;
        set => SetProperty(ref description, value ?? string.Empty);
    }

    /// <summary>
    /// Gets or sets the new vault's passphrase.
    /// </summary>
    public string Passphrase
    {
        get => passphrase;
        set => SetProperty(ref passphrase, value ?? string.Empty);
    }

    /// <summary>
    /// Gets or sets the passphrase entered again, to confirm it.
    /// </summary>
    public string ConfirmPassphrase
    {
        get => confirmPassphrase;
        set => SetProperty(ref confirmPassphrase, value ?? string.Empty);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the user has confirmed there is no passphrase recovery.
    /// </summary>
    public bool AcknowledgedNoRecovery
    {
        get => acknowledgedNoRecovery;
        set => SetProperty(ref acknowledgedNoRecovery, value);
    }

    /// <summary>
    /// Gets a value indicating whether the vault is being created.
    /// </summary>
    public bool IsWorking
    {
        get => isWorking;
        private set => SetProperty(ref isWorking, value);
    }

    /// <summary>
    /// Gets why the vault was not created, or null.
    /// </summary>
    public string? DialogError
    {
        get => dialogError;
        private set => SetProperty(ref dialogError, value);
    }

    /// <summary>
    /// Gets the address of a vault's page.
    /// </summary>
    /// <param name="id">The vault's id.</param>
    /// <returns>The address.</returns>
    public static string GetVaultUrl(string id) => $"{Url}/{Uri.EscapeDataString(id)}";

    /// <summary>
    /// Gets a value indicating whether a vault is shown by its id, because its name is not known on this device yet.
    /// </summary>
    /// <param name="vault">The vault.</param>
    /// <returns>True if it is shown by its id.</returns>
    public static bool IsShownById(Vault vault)
    {
        ArgumentNullException.ThrowIfNull(vault);
        return vault.Name == vault.Id;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Shows the vaults known now, then reads the list again, so vaults created or deleted on another device since
    /// sign in are added or gone (the list changes when the read finishes). Offline, the vaults known stay.
    /// </remarks>
    public override async Task OnInitializedAsync()
    {
        await RefreshAsync();
        IsLoading = false;
        if (!profiles.IsLoaded)
        {
            // Still being read after sign in; the list changes when that finishes.
            return;
        }

        try
        {
            await profiles.LoadAsync();
        }
        catch (Exception)
        {
            // Offline, or the list could not be read: the vaults known stay, and the next visit tries again.
        }
    }

    /// <summary>
    /// Opens a vault's page. A locked vault that can be unlocked with biometrics is synced (online) and unlocked with
    /// them first, so its page opens straight onto its secrets; if the passkey is cancelled or fails, the page asks for
    /// the passphrase as usual.
    /// </summary>
    /// <param name="id">The vault's id.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RelayCommand]
    public async Task OpenAsync(string id)
    {
        if (Vaults.FirstOrDefault(v => v.Id == id) is { CanUnlockWithBiometrics: true, IsUnlocked: false })
        {
            try
            {
                await scheduler.SyncProfileAsync(id);
            }
            catch (Exception)
            {
                // Unlocking goes ahead with the files this device has.
            }

            await vaultService.UnlockWithBiometricsAsync(id);
        }

        navigationService.NavigateTo(GetVaultUrl(id));
    }

    /// <summary>
    /// Stops unlocking a vault with biometrics on this device.
    /// </summary>
    /// <param name="id">The vault's id.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RelayCommand]
    public async Task ForgetBiometricsAsync(string id)
    {
        try
        {
            await vaultService.ForgetBiometricsAsync(id);
        }
        finally
        {
            await RefreshSafelyAsync();
        }
    }

    /// <summary>
    /// Locks a vault.
    /// </summary>
    /// <param name="id">The vault's id.</param>
    [RelayCommand]
    public void Lock(string id)
    {
        vaultService.Lock(id);
    }

    /// <summary>
    /// Opens the new vault dialog with empty fields.
    /// </summary>
    [RelayCommand]
    public void ShowCreateDialog()
    {
        ClearInputs();
        Name = string.Empty;
        Description = string.Empty;
        DialogError = null;
        IsCreateDialogOpen = true;
    }

    /// <summary>
    /// Closes the new vault dialog. Ignored while the vault is being created.
    /// </summary>
    [RelayCommand]
    public void CloseCreateDialog()
    {
        if (IsWorking)
        {
            return;
        }

        ClearInputs();
        IsCreateDialogOpen = false;
    }

    /// <summary>
    /// Creates the vault, once the dialog's fields are valid, and opens it.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RelayCommand]
    public async Task CreateAsync()
    {
        if (IsWorking)
        {
            return;
        }

        DialogError = Validate();
        if (DialogError != null)
        {
            return;
        }

        IsWorking = true;
        try
        {
            var vault = await vaultService.CreateAsync(Name, Description, Passphrase);
            ClearInputs();
            IsCreateDialogOpen = false;
            navigationService.NavigateTo(GetVaultUrl(vault.Id));
        }
        catch (Exception ex)
        {
            DialogError = errorText.Describe(ex);
        }
        finally
        {
            IsWorking = false;
        }
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            profiles.ProfilesChanged -= OnProfilesChanged;
            manager.LockStateChanged -= OnLockStateChanged;
        }

        base.Dispose(disposing);
    }

    private string? Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            return errorText.Describe(Exceptions.ClypseErrorCodes.VaultNameRequired, []);
        }

        if (string.IsNullOrEmpty(Passphrase))
        {
            return text["Vaults.Create.PassphraseRequired"];
        }

        if (!string.Equals(Passphrase, ConfirmPassphrase, StringComparison.Ordinal))
        {
            return text["Vaults.Create.PassphraseMismatch"];
        }

        return AcknowledgedNoRecovery ? null : text["Vaults.Create.AcknowledgeRequired"].Value;
    }

    private void ClearInputs()
    {
        Passphrase = string.Empty;
        ConfirmPassphrase = string.Empty;
        AcknowledgedNoRecovery = false;
    }

    private async Task RefreshAsync()
    {
        var all = await vaultService.GetVaultsAsync();
        Vaults = [.. all.OrderBy(v => IsShownById(v)).ThenBy(v => v.Name, StringComparer.CurrentCultureIgnoreCase)];
    }

    private async void OnProfilesChanged(object? sender, EventArgs e)
    {
        await RefreshSafelyAsync();
    }

    private async void OnLockStateChanged(object? sender, E2eKeyScope scope)
    {
        await RefreshSafelyAsync();
    }

    private async Task RefreshSafelyAsync()
    {
        try
        {
            await RefreshAsync();
        }
        catch (Exception)
        {
            // A background refresh must not crash the page; the next change refreshes it again.
        }
    }
}
