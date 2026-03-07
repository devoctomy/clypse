using clypse.core.Cloud.Aws.S3;
using clypse.portal.Application.Services.Interfaces;
using Microsoft.JSInterop;

namespace clypse.portal.Services;

/// <inheritdoc/>
public class JsS3InvokerProvider(IJSRuntime jsRuntime) : IJsS3InvokerProvider
{
    private readonly IJSRuntime jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));

    /// <inheritdoc/>
    public IJavaScriptS3Invoker GetInvoker()
    {
        return new JavaScriptS3Invoker(jsRuntime);
    }
}
