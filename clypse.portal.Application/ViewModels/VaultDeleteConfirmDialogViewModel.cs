using Blazing.Mvvm.ComponentModel;
using clypse.portal.Models.Vault;

namespace clypse.portal.Application.ViewModels;

/// <summary>
/// ViewModel for the delete vault confirmation dialog.
/// </summary>
public partial class VaultDeleteConfirmDialogViewModel : ViewModelBase
{
    private string confirmationText = string.Empty;
    private VaultMetadata? vaultToDelete;

    /// <summary>Gets or sets the text typed by the user to confirm deletion.</summary>
    public string ConfirmationText
    {
        get => confirmationText;
        set
        {
            SetProperty(ref confirmationText, value);
            OnPropertyChanged(nameof(IsConfirmationValid));
        }
    }

    /// <summary>Gets or sets the vault that will be deleted.</summary>
    public VaultMetadata? VaultToDelete
    {
        get => vaultToDelete;
        set
        {
            SetProperty(ref vaultToDelete, value);
            OnPropertyChanged(nameof(ExpectedConfirmationText));
        }
    }

    /// <summary>Gets or sets the error message.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Gets or sets whether a deletion is in progress.</summary>
    public bool IsDeleting { get; set; }

    /// <summary>Gets the text the user must type to confirm deletion.</summary>
    public string ExpectedConfirmationText
    {
        get
        {
            if (VaultToDelete == null)
            {
                return string.Empty;
            }

            return !string.IsNullOrEmpty(VaultToDelete.Name) ? VaultToDelete.Name : VaultToDelete.Id;
        }
    }

    /// <summary>Gets a value indicating whether the user confirmation text matches the expected value.</summary>
    public bool IsConfirmationValid =>
        VaultToDelete != null &&
        ConfirmationText.Trim().Equals(ExpectedConfirmationText, StringComparison.Ordinal);

    /// <summary>
    /// Resets the confirmation text (called when the dialog is hidden).
    /// </summary>
    public void Reset()
    {
        ConfirmationText = string.Empty;
    }
}
