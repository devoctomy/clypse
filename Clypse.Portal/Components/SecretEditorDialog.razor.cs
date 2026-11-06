using Blazing.Mvvm.Components;
using Clypse.Portal.Application.ViewModels;
using Microsoft.AspNetCore.Components;

namespace Clypse.Portal.Components;

/// <summary>
/// Code-behind for the secret editor. Logic lives in <see cref="SecretEditorViewModel"/>; this only passes the secret
/// in, and says when the editor is done.
/// </summary>
public partial class SecretEditorDialog : MvvmComponentBase<SecretEditorViewModel>
{
    private bool loaded;
    private string? loadedId;

    /// <summary>
    /// Gets or sets the id of the secret to edit, or null for a new secret.
    /// </summary>
    [Parameter]
    public string? SecretId { get; set; }

    /// <summary>
    /// Gets or sets what happens when the editor is closed, or the secret saved or deleted.
    /// </summary>
    [Parameter]
    public EventCallback OnClose { get; set; }

    /// <inheritdoc/>
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        if (!loaded || loadedId != SecretId)
        {
            loaded = true;
            loadedId = SecretId;
            await ViewModel.LoadAsync(SecretId);
        }
    }

    private async Task SaveAsync()
    {
        if (await ViewModel.SaveAsync())
        {
            await OnClose.InvokeAsync();
        }
    }

    private async Task DeleteAsync()
    {
        if (await ViewModel.DeleteAsync())
        {
            await OnClose.InvokeAsync();
        }
    }

    private Task CloseAsync() => ViewModel.IsWorking ? Task.CompletedTask : OnClose.InvokeAsync();
}
