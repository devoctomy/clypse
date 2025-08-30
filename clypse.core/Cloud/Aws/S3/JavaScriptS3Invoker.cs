using Microsoft.JSInterop;

namespace clypse.core.Cloud.Aws.S3;

/// <summary>
/// Implementation of IJavaScriptS3Invoker that wraps IJSRuntime for JavaScript S3 operations.
/// </summary>
public class JavaScriptS3Invoker : IJavaScriptS3Invoker
{
    private readonly IJSRuntime jsRuntime;

    /// <summary>
    /// Initializes a new instance of the <see cref="JavaScriptS3Invoker"/> class.
    /// </summary>
    /// <param name="jsRuntime">The JavaScript runtime for interop calls.</param>
    public JavaScriptS3Invoker(IJSRuntime jsRuntime)
    {
        this.jsRuntime = jsRuntime;
    }

    /// <summary>
    /// Invokes a JavaScript S3 operation asynchronously.
    /// </summary>
    /// <param name="functionName">The name of the JavaScript function to invoke.</param>
    /// <param name="request">The request object to pass to the JavaScript function.</param>
    /// <returns>The result of the S3 operation.</returns>
    public async Task<S3OperationResult> InvokeS3OperationAsync(string functionName, object request)
    {
        return await this.jsRuntime.InvokeAsync<S3OperationResult>(functionName, request);
    }
}
