using Microsoft.AspNetCore.Components;
using Blazing.Mvvm.Components;
using clypse.portal.Application.ViewModels;
using clypse.portal.Models.Vault;

namespace clypse.portal.Components;

/// <summary>
/// Code-behind for the delete vault confirmation dialog. Business logic is in <see cref="VaultDeleteConfirmDialogViewModel"/>.
/// </summary>
public partial class VaultDeleteConfirmDialog : MvvmComponentBase<VaultDeleteConfirmDialogViewModel>
{
    [Parameter] public bool Show { get; set; }
    [Parameter] public VaultMetadata? VaultToDelete { get; set; }
    [Parameter] public bool IsDeleting { get; set; }
    [Parameter] public string? ErrorMessage { get; set; }
    [Parameter] public EventCallback OnConfirm { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }

    protected override void OnParametersSet()
    {
        ViewModel.VaultToDelete = VaultToDelete;
        ViewModel.IsDeleting = IsDeleting;
        ViewModel.ErrorMessage = ErrorMessage;

        if (!Show)
        {
            ViewModel.Reset();
        }
    }
}
