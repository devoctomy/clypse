using clypse.core.Exceptions;

namespace clypse.core.Vault.Exceptions;

/// <summary>
/// Represents errors that occur when attempting to load vault index from storage.
/// </summary>
public class FailedToLoadVaultIndexException : ClypseCoreException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FailedToLoadVaultIndexException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public FailedToLoadVaultIndexException(string message)
        : base(message)
    {
    }
}
