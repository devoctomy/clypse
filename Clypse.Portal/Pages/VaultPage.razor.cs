using Blazing.Mvvm.Components;
using Clypse.Portal.Application.ViewModels;
using Microsoft.AspNetCore.Components;

namespace Clypse.Portal.Pages;

/// <summary>
/// Code-behind for a vault's page. Logic lives in <see cref="VaultViewModel"/>.
/// </summary>
public partial class VaultPage : MvvmComponentBase<VaultViewModel>
{
    /// <summary>
    /// Gets or sets the vault's id, from the address.
    /// </summary>
    [Parameter]
    public string Id { get; set; } = string.Empty;

    /// <inheritdoc/>
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        await ViewModel.LoadAsync(Id);
    }
}
