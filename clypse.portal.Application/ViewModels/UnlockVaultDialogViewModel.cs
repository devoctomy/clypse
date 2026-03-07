using Blazing.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using clypse.portal.Models.Vault;

namespace clypse.portal.Application.ViewModels;

/// <summary>
/// ViewModel for the unlock vault dialog.
/// </summary>
public partial class UnlockVaultDialogViewModel : ViewModelBase
{
    private string passphrase = string.Empty;
    private VaultMetadata? vault;

    /// <summary>Gets or sets the passphrase entered by the user.</summary>
    public string Passphrase { get => passphrase; set => SetProperty(ref passphrase, value); }

    /// <summary>Gets or sets the vault being unlocked.</summary>
    public VaultMetadata? Vault { get => vault; set => SetProperty(ref vault, value); }

    /// <summary>Gets or sets whether an unlock operation is in progress.</summary>
    public bool IsUnlocking { get; set; }

    /// <summary>Gets or sets the error message if unlocking failed.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Gets or sets the callback invoked to perform the unlock.</summary>
    public Func<string, Task>? OnUnlockCallback { get; set; }

    /// <summary>Gets or sets the callback invoked when the user cancels.</summary>
    public Func<Task>? OnCancelCallback { get; set; }

    /// <summary>
    /// Clears the passphrase (called when the dialog is shown).
    /// </summary>
    public void ResetPassphrase()
    {
        Passphrase = string.Empty;
    }

    /// <summary>Attempts to unlock with the current passphrase.</summary>
    [RelayCommand]
    public async Task UnlockAsync()
    {
        if (!string.IsNullOrEmpty(Passphrase) && OnUnlockCallback != null)
        {
            await OnUnlockCallback(Passphrase);
        }
    }

    /// <summary>Cancels the unlock dialog.</summary>
    [RelayCommand]
    public async Task CancelAsync()
    {
        Passphrase = string.Empty;
        if (OnCancelCallback != null)
        {
            await OnCancelCallback();
        }
    }
}
