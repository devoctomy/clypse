using clypse.core.Exceptions;

namespace clypse.core.Vault.Exceptions;

public class FailedToLoadVaultIndexException : ClypseCoreException
{
    public FailedToLoadVaultIndexException(string message) :
        base(message)
    {
    }
}
