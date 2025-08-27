namespace clypse.core.Exceptions;

public class ClypseCoreException : Exception
{
    public ClypseCoreException(string message)
        : base(message)
    {
    }

    public ClypseCoreException(
        string message,
        Exception innerException)
        : base(message, innerException)
    {
    }
}