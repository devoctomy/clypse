using Clypse.Portal.Application.Services.Interfaces;
using Clypse.Portal.Models.Files;
using Microsoft.JSInterop;

namespace Clypse.Portal.Services;

/// <summary>
/// Implementation of <see cref="ITextEditorInteropService"/> using the functions in <c>wwwroot/js/editor.js</c>.
/// </summary>
/// <param name="jsRuntime">Calls into the browser.</param>
public class TextEditorInteropService(IJSRuntime jsRuntime) : ITextEditorInteropService
{
    private readonly IJSRuntime jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));

    /// <inheritdoc/>
    public async Task<EditorSelection?> GetSelectionAsync(string elementId)
    {
        return await jsRuntime.InvokeAsync<EditorSelection?>("clypseEditor.getSelection", elementId);
    }

    /// <inheritdoc/>
    public async Task ApplyEditAsync(string elementId, MarkdownTextEdit edit)
    {
        await jsRuntime.InvokeVoidAsync("clypseEditor.applyEdit", elementId, edit);
    }
}
