using clypse.core.Cloud;
using clypse.core.Cloud.Aws.S3;
using clypse.core.Vault;
using clypse.portal.Application.Services.Interfaces;

namespace clypse.portal.Application.Services;

public class VaultManagerBootstrapperFactoryService : IVaultManagerBootstrapperFactoryService
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
        string identityId)
    {
        var jsS3Client = new JavaScriptS3Client(
            jsInvoker,
            accessKey,
            secretAccessKey,
            sessionToken,
            region);
        
        // Create the plain cloud storage provider (not encrypted) for the bootstrapper
        var awsCloudStorageProvider = new AwsCloudStorageProviderBase(
            bucketName,
            jsS3Client);
        
        var bootstrapperService = new AwsS3VaultManagerBootstrapperService(
            identityId,
            awsCloudStorageProvider);
        
        return bootstrapperService;
    }
}
