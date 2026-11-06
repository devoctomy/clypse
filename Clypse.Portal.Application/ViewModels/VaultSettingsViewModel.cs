using Blazing.Mvvm.ComponentModel;
using Clypse.Portal.Application.Services.Interfaces;
using Clypse.Portal.Application.Text;
using Clypse.Portal.Models.Vaults;
using Devoctomy.Blazwiz.Browser.Services.Interfaces;
using Devoctomy.Blazwiz.Core.Text.Interfaces;
using Devoctomy.Blazwiz.Encryption.Models;
using Devoctomy.Blazwiz.Sync.Encryption.Interfaces;
using Devoctomy.Blazwiz.UI.Models;
using Devoctomy.Blazwiz.UI.Services.Interfaces;
using Devoctomy.Blazwiz.UI.Text.Interfaces;
using Microsoft.Extensions.Localization;

namespace Clypse.Portal.Application.ViewModels;

/// <summary>
/// ViewModel for a vault's settings: its name and description, how long it stays unlocked when it is not used (kept
/// with the vault, so on every device), and deleting it, once its name is typed to confirm.
/// </summary>
public partial class VaultSettingsViewModel : ViewModelBase
{
    /// <summary>
    /// The auto-lock times offered, in minutes; null is never.
    /// </summary>
    public static readonly IReadOnlyList<int?> AutoLockMinutes = [1, 5, 15, 30, 60, null];

    private readonly IVaultService vaultService;
    private readonly ISyncProtectedProfileService profiles;
    private readonly INavigationService navigationService;
    private readonly IToastService toasts;
    private readonly IErrorText errorText;
    private readonly ITextPlurals plurals;
    private readonly IStringLocalizer<ClypseText> text;

    private Vault? vault;
    private string name = string.Empty;
    private string description = string.Empty;
    private int? autoLock;
    private int? savedAutoLock;
    private IReadOnlyList<int?> autoLockOptions = AutoLockMinutes;
    private string deleteConfirmation = string.Empty;
    private bool isWorking;
    private string? error;

    /// <summary>
    /// Initializes a new instance of the <see cref="VaultSettingsViewModel"/> class.
    /// </summary>
    /// <param name="vaultService">Renames and deletes the vault.</param>
    /// <param name="profiles">Keeps the vault's idle timeout.</param>
    /// <param name="navigationService">Goes back to the vaults once the vault is deleted.</param>
    /// <param name="toasts">Says what was done.</param>
    /// <param name="errorText">Describes errors.</param>
    /// <param name="plurals">Words counts.</param>
    /// <param name="text">Clypse's text.</param>
    public VaultSettingsViewModel(
        IVaultService vaultService,
        ISyncProtectedProfileService profiles,
        INavigationService navigationService,
        IToastService toasts,
        IErrorText errorText,
        ITextPlurals plurals,
        IStringLocalizer<ClypseText> text)
    {
        this.vaultService = vaultService ?? throw new ArgumentNullException(nameof(vaultService));
        this.profiles = profiles ?? throw new ArgumentNullException(nameof(profiles));
        this.navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        this.toasts = toasts ?? throw new ArgumentNullException(nameof(toasts));
        this.errorText = errorText ?? throw new ArgumentNullException(nameof(errorText));
        this.plurals = plurals ?? throw new ArgumentNullException(nameof(plurals));
        this.text = text ?? throw new ArgumentNullException(nameof(text));
    }

    /// <summary>
    /// Gets or sets the vault's name.
    /// </summary>
    public string Name
    {
        get => name;
        set => SetProperty(ref name, value ?? string.Empty);
    }

    /// <summary>
    /// Gets or sets the vault's description.
    /// </summary>
    public string Description
    {
        get => description;
        set => SetProperty(ref description, value ?? string.Empty);
    }

    /// <summary>
    /// Gets or sets how long, in minutes, the vault stays unlocked when it is not used; null for never.
    /// </summary>
    public int? AutoLock
    {
        get => autoLock;
        set => SetProperty(ref autoLock, value);
    }

    /// <summary>
    /// Gets the auto-lock times to choose from, in minutes (null is never), including the vault's own if it is not one
    /// of <see cref="AutoLockMinutes"/>.
    /// </summary>
    public IReadOnlyList<int?> AutoLockOptions
    {
        get => autoLockOptions;
        private set => SetProperty(ref autoLockOptions, value);
    }

    /// <summary>
    /// Gets or sets the name typed to confirm deleting the vault.
    /// </summary>
    public string DeleteConfirmation
    {
        get => deleteConfirmation;
        set => SetProperty(ref deleteConfirmation, value ?? string.Empty);
    }

    /// <summary>
    /// Gets a value indicating whether the settings are being saved or the vault deleted.
    /// </summary>
    public bool IsWorking
    {
        get => isWorking;
        private set => SetProperty(ref isWorking, value);
    }

    /// <summary>
    /// Gets why the settings were not saved or the vault not deleted, or null.
    /// </summary>
    public string? Error
    {
        get => error;
        private set => SetProperty(ref error, value);
    }

    /// <summary>
    /// Words an auto-lock time, e.g. "5 minutes" or "Never".
    /// </summary>
    /// <param name="minutes">The time, in minutes; null for never.</param>
    /// <returns>The text.</returns>
    public string GetAutoLockText(int? minutes) => minutes is { } value
        ? plurals.Format(text, "VaultSettings.AutoLock.Minutes", value)
        : text["VaultSettings.AutoLock.Never"];

    /// <summary>
    /// Starts editing a vault's settings.
    /// </summary>
    /// <param name="target">The vault.</param>
    public void Load(Vault target)
    {
        vault = target ?? throw new ArgumentNullException(nameof(target));
        Name = target.Name;
        Description = target.Description ?? string.Empty;
        DeleteConfirmation = string.Empty;
        Error = null;

        var timeout = profiles.Profiles.FirstOrDefault(p => p.Name == target.Id)?.Settings.IdleTimeout;
        savedAutoLock = timeout is { } span ? (int)Math.Max(1, Math.Round(span.TotalMinutes)) : null;
        AutoLock = savedAutoLock;
        AutoLockOptions = AutoLockMinutes.Contains(savedAutoLock)
            ? AutoLockMinutes
            : [.. AutoLockMinutes.Where(m => m != null).Append(savedAutoLock).Order(), null];
    }

    /// <summary>
    /// Saves what changed: the name and description, and the auto-lock time.
    /// </summary>
    /// <returns>True if they were saved.</returns>
    public async Task<bool> SaveAsync()
    {
        if (vault == null || IsWorking)
        {
            return false;
        }

        IsWorking = true;
        Error = null;
        try
        {
            var newDescription = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim();
            if (Name.Trim() != vault.Name || newDescription != vault.Description)
            {
                var updated = await vaultService.UpdateAsync(vault.Id, Name, Description);
                vault.Name = updated.Name;
                vault.Description = updated.Description;
            }

            if (AutoLock != savedAutoLock)
            {
                var timeout = AutoLock is { } minutes ? TimeSpan.FromMinutes(minutes) : (TimeSpan?)null;
                await profiles.SetIdleTimeoutAsync(new E2eKeyScope(vault.Id), timeout);
                savedAutoLock = AutoLock;
            }

            toasts.Show(text["VaultSettings.Saved"], ToastTone.Success);
            return true;
        }
        catch (Exception ex)
        {
            Error = errorText.Describe(ex);
            return false;
        }
        finally
        {
            IsWorking = false;
        }
    }

    /// <summary>
    /// Deletes the vault, once its name has been typed exactly, and goes back to the vaults.
    /// </summary>
    /// <returns>True if it was deleted.</returns>
    public async Task<bool> DeleteAsync()
    {
        if (vault == null || IsWorking)
        {
            return false;
        }

        if (!string.Equals(DeleteConfirmation.Trim(), vault.Name, StringComparison.Ordinal))
        {
            Error = text["VaultSettings.Delete.Mismatch"];
            return false;
        }

        IsWorking = true;
        Error = null;
        try
        {
            await vaultService.DeleteAsync(vault.Id);
            toasts.Show(text["VaultSettings.Deleted", vault.Name], ToastTone.Success);
            navigationService.NavigateTo(VaultsViewModel.Url);
            return true;
        }
        catch (Exception ex)
        {
            Error = errorText.Describe(ex);
            return false;
        }
        finally
        {
            IsWorking = false;
        }
    }
}
