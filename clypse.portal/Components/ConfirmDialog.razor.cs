using Microsoft.AspNetCore.Components;
using Blazing.Mvvm.Components;
using clypse.portal.Application.ViewModels;

namespace clypse.portal.Components;

/// <summary>
/// Code-behind for the generic confirm dialog. Business logic is in <see cref="ConfirmDialogViewModel"/>.
/// </summary>
public partial class ConfirmDialog : MvvmComponentBase<ConfirmDialogViewModel>
{
    [Parameter] public bool Show { get; set; }
    [Parameter] public string Message { get; set; } = "Are you sure you want to delete this item?";
    [Parameter] public bool IsProcessing { get; set; }
    [Parameter] public EventCallback OnConfirm { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }

    protected override void OnParametersSet()
    {
        ViewModel.IsProcessing = IsProcessing;
        ViewModel.Message = Message;
        ViewModel.OnConfirmCallback = () => OnConfirm.InvokeAsync();
        ViewModel.OnCancelCallback = () => OnCancel.InvokeAsync();
    }
}
