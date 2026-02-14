using clypse.core.Cloud.Aws.S3;
using clypse.core.Vault;

namespace clypse.portal.Application.Services.Interfaces;

/// <summary>
/// Factory service for IVaultManagerBootstrapperService.
/// </summary>
public interface IVaultManagerBootstrapperFactoryService
{
    /// <summary>
    /// Create an instance of IVaultManagerBootstrapperService that is suitable for use with Blazor.
    /// </summary>
    /// <param name="jsInvoker">The JavaScript S3 invoker for interop calls.</param>
    /// <param name="accessKey">AWS access key ID.</param>
    /// <param name="secretAccessKey">AWS secret access key.</param>
    /// <param name="sessionToken">AWS session token (for temporary credentials).</param>
    /// <param name="region">AWS region name.</param>
    /// <param name="bucketName">Name of S3 bucket where data is stored.</param>
    /// <param name="identityId">Cognito Identity Id of user that owns the vault.</param>
    /// <returns>Instance of IVaultManagerBootstrapperService.</returns>
    public IVaultManagerBootstrapperService CreateForBlazor(
        IJavaScriptS3Invoker jsInvoker,
        string accessKey,
        string secretAccessKey,
        string sessionToken,
        string region,
        string bucketName,
        string identityId);
}
