using clypse.core.Exceptions;

namespace clypse.core.Cloud.Exceptions;

public class CloudStorageProviderException : ClypseCoreException
{
    public CloudStorageProviderException(string message)
        : base(message)
    {
    }

    public CloudStorageProviderException(
        string message,
        Exception innerException)
        : base(message, innerException)
    {
    }
}
