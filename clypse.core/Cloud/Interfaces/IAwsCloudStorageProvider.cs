using clypse.core.Cryptogtaphy.Interfaces;

namespace clypse.core.Cloud.Interfaces;

/// <summary>
/// Defines a transformer interface for creating different types of AWS encrypted cloud storage providers.
/// </summary>
public interface IAwsEncryptedCloudStorageProviderTransformer
{

    /// <summary>
    /// Creates an end-to-end encrypted AWS S3 cloud storage provider using the specified cryptographic service.
    /// </summary>
    /// <param name="cryptoService">Cryptographic service to use for the encryption process.</param>
    /// <returns>Instance of AwsS3E2eCloudStorageProvider.</returns>
    AwsS3E2eCloudStorageProvider CreateE2eProvider(ICryptoService cryptoService);

    /// <summary>
    /// Creates an server-side encrypted AWS S3 cloud storage provider using the specified cryptographic service.
    /// </summary>
    /// <returns>Instance of AwsS3SseCCloudStorageProvider.</returns>
    AwsS3SseCCloudStorageProvider CreateSseProvider();
}
