using clypse.core.Cloud;
using clypse.core.Cloud.Aws.S3;
using clypse.core.Vault;
using clypse.portal.Application.Services.Interfaces;

namespace clypse.portal.Application.Services;

/// <inheritdoc/>
public class VaultManagerBootstrapperFactoryService : IVaultManagerBootstrapperFactoryService
{
    /// <inheritdoc/>
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
