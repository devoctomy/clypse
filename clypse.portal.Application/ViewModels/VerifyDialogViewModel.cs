using Blazing.Mvvm.ComponentModel;
using clypse.core.Vault;

namespace clypse.portal.Application.ViewModels;

/// <summary>
/// ViewModel for the vault verify results dialog — display-only.
/// </summary>
public partial class VerifyDialogViewModel : ViewModelBase
{
    private VaultVerifyResults? results;

    /// <summary>Gets or sets the verification results to display.</summary>
    public VaultVerifyResults? Results { get => results; set => SetProperty(ref results, value); }

    /// <summary>Gets or sets the callback invoked when the dialog is closed.</summary>
    public Func<Task>? OnCloseCallback { get; set; }
}
