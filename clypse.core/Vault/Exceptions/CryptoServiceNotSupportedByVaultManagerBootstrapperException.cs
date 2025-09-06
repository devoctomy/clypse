using clypse.core.Exceptions;

namespace clypse.core.Vault.Exceptions;

/// <summary>
/// Represents errors that occur when the encrypted cloud storage provider is not supported by the vault manager bootstrapper.
/// </summary>
public class CryptoServiceNotSupportedByVaultManagerBootstrapperException : ClypseCoreException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CryptoServiceNotSupportedByVaultManagerBootstrapperException"/> class.
    /// </summary>
    /// <param name="cryptoServiceName">The crypto service name which caused the exception.</param>
    public CryptoServiceNotSupportedByVaultManagerBootstrapperException(string? cryptoServiceName)
        : base($"The crypto service '{cryptoServiceName}' is not supported by VaultManagerBootstrapper.")
    {
    }
}
