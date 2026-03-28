using Microsoft.AspNetCore.Components;
using Blazing.Mvvm.Components;
using clypse.portal.Application.ViewModels;

namespace clypse.portal.Components;

/// <summary>
/// Code-behind for the loading dialog. Logic is in <see cref="LoadingDialogViewModel"/>.
/// </summary>
public partial class LoadingDialog : MvvmComponentBase<LoadingDialogViewModel>
{
    [Parameter] public bool Show { get; set; }
    [Parameter] public string Message { get; set; } = "Loading...";

    protected override void OnParametersSet()
    {
        ViewModel.Message = Message;
    }
}
