using clypse.core.Exceptions;

namespace clypse.core.Vault.Exceptions;

/// <summary>
/// Represents errors that occur when attempting to load vault information from storage.
/// </summary>
public class FailedToLoadVaultInfoException : ClypseCoreException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FailedToLoadVaultInfoException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public FailedToLoadVaultInfoException(string message)
        : base(message)
    {
    }
}
