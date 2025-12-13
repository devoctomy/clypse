namespace clypse.core.Cloud.Aws.S3;

/// <summary>
/// Interface for invoking JavaScript S3 operations.
/// This abstraction allows for easier testing by wrapping the IJSRuntime extension methods.
/// </summary>
public interface IJavaScriptS3Invoker
{
    /// <summary>
    /// Invokes a JavaScript S3 operation asynchronously.
    /// </summary>
    /// <param name="functionName">The name of the JavaScript function to invoke.</param>
    /// <param name="request">The request object to pass to the JavaScript function.</param>
    /// <returns>The result of the S3 operation.</returns>
    Task<S3OperationResult> InvokeS3OperationAsync(string functionName, object request);
}
