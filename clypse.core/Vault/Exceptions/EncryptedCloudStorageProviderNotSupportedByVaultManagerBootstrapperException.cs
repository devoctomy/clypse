using clypse.core.Exceptions;

namespace clypse.core.Vault.Exceptions;

/// <summary>
/// Represents errors that occur when the encrypted cloud storage provider is not supported by the vault manager bootstrapper.
/// </summary>
public class EncryptedCloudStorageProviderNotSupportedByVaultManagerBootstrapperException : ClypseCoreException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EncryptedCloudStorageProviderNotSupportedByVaultManagerBootstrapperException"/> class.
    /// </summary>
    /// <param name="encryptedCloudStorageProviderName">The encrypted cloud storage provider name which caused the exception.</param>
    public EncryptedCloudStorageProviderNotSupportedByVaultManagerBootstrapperException(string? encryptedCloudStorageProviderName)
        : base($"The encrypted cloud storage provider '{encryptedCloudStorageProviderName}' is not supported by VaultManagerBootstrapper.")
    {
    }
}
