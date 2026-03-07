using Microsoft.AspNetCore.Components;
using Blazing.Mvvm.Components;
using clypse.core.Vault;
using clypse.portal.Application.ViewModels;

namespace clypse.portal.Components;

/// <summary>
/// Code-behind for the verify dialog. Logic is in <see cref="VerifyDialogViewModel"/>.
/// </summary>
public partial class VerifyDialog : MvvmComponentBase<VerifyDialogViewModel>
{
    [Parameter] public bool Show { get; set; }
    [Parameter] public VaultVerifyResults? Results { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }

    protected override void OnParametersSet()
    {
        ViewModel.Results = Results;
        ViewModel.OnCloseCallback = () => OnClose.InvokeAsync();
    }
}
