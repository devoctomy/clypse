using clypse.core.Exceptions;

namespace clypse.core.Vault.Exceptions;

public class FailedToLoadVaultInfoException : ClypseCoreException
{
    public FailedToLoadVaultInfoException(string message)
        : base(message)
    {
    }
}
