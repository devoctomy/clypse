using clypse.core.Cloud;
using clypse.core.Cloud.Aws.S3;
using clypse.core.Compression;
using clypse.core.Cryptography;
using clypse.core.Vault;
using clypse.portal.Application.Services.Interfaces;

namespace clypse.portal.Application.Services;

public class VaultManagerFactoryService(KeyDerivationServiceOptions keyDerivationServiceOptions) : IVaultManagerFactoryService
{
    /// <summary>
    /// Create an instance of IVaultManager that is suitable for use with Blazor.
    /// </summary>
    /// <param name="jsInvoker">The JavaScript S3 invoker for interop calls.</param>
    /// <param name="accessKey">AWS access key ID.</param>
    /// <param name="secretKey">AWS secret access key.</param>
    /// <param name="sessionToken">AWS session token (for temporary credentials).</param>
    /// <param name="region">AWS region name.</param>
    /// <param name="bucketName">Name of S3 bucket where data is stored.</param>
    /// <param name="identityId">Cognito Identity Id of user that owns the vault.</param>
    /// <returns>Instance of IVaultManager.</returns>
    public IVaultManager CreateForBlazor(
        IJavaScriptS3Invoker jsInvoker,
        string accessKey,
        string secretAccessKey,
        string sessionToken,
        string region,
        string bucketName,
        string identityId)
    {
        var keyDerivationService = new KeyDerivationService(
            new RandomGeneratorService(),
            keyDerivationServiceOptions);
        var jsS3Client = new JavaScriptS3Client(
            jsInvoker,
            accessKey,
            secretAccessKey,
            sessionToken,
            region);
        var awsS3E2eCloudStorageProvider = new AwsS3E2eCloudStorageProvider(
            bucketName,
            jsS3Client,
            new BouncyCastleAesGcmCryptoService());
        var vaultManager = new VaultManager(
            identityId,
            keyDerivationService,
            new GZipCompressionService(),
            awsS3E2eCloudStorageProvider);
        return vaultManager;
    }
}
