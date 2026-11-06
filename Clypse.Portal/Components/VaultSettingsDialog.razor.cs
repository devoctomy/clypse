using Blazing.Mvvm.Components;
using Clypse.Portal.Application.ViewModels;
using Clypse.Portal.Models.Vaults;
using Microsoft.AspNetCore.Components;

namespace Clypse.Portal.Components;

/// <summary>
/// Code-behind for a vault's settings. Logic lives in <see cref="VaultSettingsViewModel"/>; this only passes the vault
/// in, and says when the dialog is done.
/// </summary>
public partial class VaultSettingsDialog : MvvmComponentBase<VaultSettingsViewModel>
{
    private Vault? loaded;

    /// <summary>
    /// Gets or sets the vault.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public Vault Vault { get; set; } = null!;

    /// <summary>
    /// Gets or sets what happens when the dialog is closed, or the settings saved.
    /// </summary>
    [Parameter]
    public EventCallback OnClose { get; set; }

    /// <inheritdoc/>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (!ReferenceEquals(loaded, Vault))
        {
            loaded = Vault;
            ViewModel.Load(Vault);
        }
    }

    private async Task SaveAsync()
    {
        if (await ViewModel.SaveAsync())
        {
            await OnClose.InvokeAsync();
        }
    }

    private Task DeleteAsync() => ViewModel.DeleteAsync();

    private Task CloseAsync() => ViewModel.IsWorking ? Task.CompletedTask : OnClose.InvokeAsync();
}
