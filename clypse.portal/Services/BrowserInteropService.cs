using clypse.portal.Application.Services.Interfaces;
using Microsoft.JSInterop;

namespace clypse.portal.Services;

/// <inheritdoc/>
public class BrowserInteropService(IJSRuntime jsRuntime) : IBrowserInteropService
{
    private readonly IJSRuntime jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));

    /// <inheritdoc/>
    public async Task SetThemeAsync(string theme)
    {
        await jsRuntime.InvokeVoidAsync("document.documentElement.setAttribute", "data-theme", theme);
    }
}
