using Microsoft.AspNetCore.Components;
using Blazing.Mvvm.Components;
using clypse.portal.Application.ViewModels;

namespace clypse.portal.Components;

/// <summary>
/// Code-behind for the changes dialog. Business logic is in <see cref="ChangesDialogViewModel"/>.
/// </summary>
public partial class ChangesDialog : MvvmComponentBase<ChangesDialogViewModel>
{
    [Parameter] public bool Show { get; set; }
    [Parameter] public bool ShowUpdateButton { get; set; }
    [Parameter] public string? AvailableVersion { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public EventCallback OnUpdate { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        ViewModel.OnCloseCallback = () => OnClose.InvokeAsync();
        ViewModel.OnUpdateCallback = () => OnUpdate.InvokeAsync();

        if (Show)
        {
            await ViewModel.EnsureChangelogLoadedAsync();
        }
    }
}
