using clypse.core.Cloud.Aws.S3;

namespace clypse.portal.Application.Services.Interfaces;

/// <summary>
/// Provides a JavaScript S3 invoker for performing S3 operations via browser JavaScript interop.
/// </summary>
public interface IJsS3InvokerProvider
{
    /// <summary>
    /// Creates and returns a JavaScript S3 invoker instance.
    /// </summary>
    /// <returns>A new <see cref="IJavaScriptS3Invoker"/> instance.</returns>
    IJavaScriptS3Invoker GetInvoker();
}
