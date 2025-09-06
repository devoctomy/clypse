using clypse.core.Exceptions;

namespace clypse.core.Vault.Exceptions;

/// <summary>
/// Represents errors that occur when the compression service is not supported by the vault manager bootstrapper.
/// </summary>
public class CompressionServiceNotSupportedByVaultManagerBootstrapperException : ClypseCoreException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompressionServiceNotSupportedByVaultManagerBootstrapperException"/> class.
    /// </summary>
    /// <param name="compressionServiceName">The Compression service name which caused the exception.</param>
    public CompressionServiceNotSupportedByVaultManagerBootstrapperException(string? compressionServiceName)
        : base($"The compression service '{compressionServiceName}' is not supported by VaultManagerBootstrapper.")
    {
    }
}
