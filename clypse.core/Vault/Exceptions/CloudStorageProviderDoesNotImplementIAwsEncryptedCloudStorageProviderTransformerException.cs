using clypse.core.Cloud.Interfaces;
using clypse.core.Exceptions;

namespace clypse.core.Vault.Exceptions;

/// <summary>
/// Represents errors that occur when the provided cloud storage provider does not implement IAwsEncryptedCloudStorageProviderTransformer.
/// </summary>
public class CloudStorageProviderDoesNotImplementIAwsEncryptedCloudStorageProviderTransformerException : ClypseCoreException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CloudStorageProviderDoesNotImplementIAwsEncryptedCloudStorageProviderTransformerException"/> class.
    /// </summary>
    /// <param name="cloudStorageProvider">ICloudStorageProvider which caused the exception.</param>
    public CloudStorageProviderDoesNotImplementIAwsEncryptedCloudStorageProviderTransformerException(ICloudStorageProvider cloudStorageProvider)
        : base($"The provided cloud storage provider '{cloudStorageProvider.GetType().Name}' does not implement IAwsEncryptedCloudStorageProviderTransformer.")
    {
    }
}
