using Blazing.Mvvm.ComponentModel;
using clypse.portal.Models.Vault;
using CommunityToolkit.Mvvm.Input;

namespace clypse.portal.Application.ViewModels;

/// <summary>
/// ViewModel for the create vault dialog.
/// </summary>
public partial class VaultCreateDialogViewModel : ViewModelBase
{
    private string vaultName = string.Empty;
    private string vaultDescription = string.Empty;
    private string vaultPassphrase = string.Empty;
    private string vaultPassphraseConfirm = string.Empty;

    /// <summary>Gets or sets the vault name.</summary>
    public string VaultName
    {
        get => vaultName;
        set
        {
            SetProperty(ref vaultName, value);
            OnPropertyChanged(nameof(IsFormValid));
        }
    }

    /// <summary>Gets or sets the vault description.</summary>
    public string VaultDescription
    {
        get => vaultDescription;
        set
        {
            SetProperty(ref vaultDescription, value);
            OnPropertyChanged(nameof(IsFormValid));
        }
    }

    /// <summary>Gets or sets the vault passphrase.</summary>
    public string VaultPassphrase
    {
        get => vaultPassphrase;
        set
        {
            SetProperty(ref vaultPassphrase, value);
            OnPropertyChanged(nameof(IsFormValid));
        }
    }

    /// <summary>Gets or sets the vault passphrase confirmation.</summary>
    public string VaultPassphraseConfirm
    {
        get => vaultPassphraseConfirm;
        set
        {
            SetProperty(ref vaultPassphraseConfirm, value);
            OnPropertyChanged(nameof(IsFormValid));
        }
    }

    /// <summary>Gets or sets the error message.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Gets or sets whether the vault is currently being created.</summary>
    public bool IsCreating { get; set; }

    /// <summary>Gets a value indicating whether the form has valid input.</summary>
    public bool IsFormValid =>
        !string.IsNullOrWhiteSpace(VaultName) &&
        !string.IsNullOrWhiteSpace(VaultDescription) &&
        !string.IsNullOrWhiteSpace(VaultPassphrase) &&
        !string.IsNullOrWhiteSpace(VaultPassphraseConfirm) &&
        VaultPassphrase == VaultPassphraseConfirm &&
        VaultPassphrase.Length >= 8;

    /// <summary>Gets or sets the callback invoked when vault creation is confirmed.</summary>
    public Func<VaultCreationRequest, Task>? OnCreateVaultCallback { get; set; }

    /// <summary>Gets or sets the callback invoked when the user cancels.</summary>
    public Func<Task>? OnCancelCallback { get; set; }

    /// <summary>
    /// Resets all form fields.
    /// </summary>
    public void ClearForm()
    {
        VaultName = string.Empty;
        VaultDescription = string.Empty;
        VaultPassphrase = string.Empty;
        VaultPassphraseConfirm = string.Empty;
    }

    /// <summary>Submits the create vault form.</summary>
    [RelayCommand]
    public async Task CreateVaultAsync()
    {
        if (!IsFormValid)
        {
            return;
        }

        var request = new VaultCreationRequest
        {
            Name = VaultName.Trim(),
            Description = VaultDescription.Trim(),
            Passphrase = VaultPassphrase,
        };

        if (OnCreateVaultCallback != null)
        {
            await OnCreateVaultCallback(request);
        }
    }

    /// <summary>Cancels the create vault dialog.</summary>
    [RelayCommand]
    public async Task CancelAsync()
    {
        ClearForm();
        if (OnCancelCallback != null)
        {
            await OnCancelCallback();
        }
    }
}
