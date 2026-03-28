using Microsoft.AspNetCore.Components;
using Blazing.Mvvm.Components;
using clypse.portal.Application.ViewModels;

namespace clypse.portal.Components;

/// <summary>
/// Code-behind for the password generator dialog. Business logic is in <see cref="PasswordGeneratorDialogViewModel"/>.
/// </summary>
public partial class PasswordGeneratorDialog : MvvmComponentBase<PasswordGeneratorDialogViewModel>
{
    [Parameter] public bool Show { get; set; }
    [Parameter] public EventCallback<string> OnPasswordGenerated { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }

    protected override void OnParametersSet()
    {
        ViewModel.OnPasswordGeneratedCallback = password => OnPasswordGenerated.InvokeAsync(password);
        ViewModel.OnCancelCallback = () => OnCancel.InvokeAsync();

        if (!Show)
        {
            ViewModel.Reset();
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (Show)
        {
            await ViewModel.InitializeAsync();
        }
    }
}
