using Microsoft.AspNetCore.Components;
using Blazing.Mvvm.Components;
using clypse.core.Secrets;
using clypse.portal.Application.ViewModels;
using clypse.portal.Models.Enums;

namespace clypse.portal.Components;

/// <summary>
/// Code-behind for the secret create/view/edit dialog. Business logic is in <see cref="SecretDialogViewModel"/>.
/// </summary>
public partial class SecretDialog : MvvmComponentBase<SecretDialogViewModel>
{
    [Parameter] public bool Show { get; set; }
    [Parameter] public Secret? Secret { get; set; }
    [Parameter] public CrudDialogMode Mode { get; set; } = CrudDialogMode.Create;
    [Parameter] public EventCallback<Secret> OnSave { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }

    protected override void OnParametersSet()
    {
        ViewModel.OnSaveCallback = secret => OnSave.InvokeAsync(secret);
        ViewModel.OnCancelCallback = () => OnCancel.InvokeAsync();

        if (Show && Secret != null)
        {
            ViewModel.InitializeForSecret(Secret, Mode);
        }
        else if (!Show)
        {
            ViewModel.Clear();
        }
    }
}
