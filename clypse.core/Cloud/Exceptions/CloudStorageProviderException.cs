namespace clypse.core.Cloud.Exceptions;

public class CloudStorageProviderException : Exception
{
    public CloudStorageProviderException(
        string message,
        Exception innerException) :
        base(message, innerException)
    {
    }
}
