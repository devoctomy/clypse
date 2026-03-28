using clypse.core.Cloud;
using clypse.core.Cloud.Aws.S3;
using clypse.core.Compression;
using clypse.core.Cryptography;
using clypse.core.Vault;
using clypse.portal.Application.Services.Interfaces;

namespace clypse.portal.Application.Services;

/// <inheritdoc/>
public class VaultManagerFactoryService(KeyDerivationServiceOptions keyDerivationServiceOptions)
    : IVaultManagerFactoryService
{
    /// <inheritdoc/>
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
