namespace Clypse.Core.Exceptions;

/// <summary>
/// Represents errors that occur within the Clypse core library.
/// </summary>
public class ClypseCoreException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClypseCoreException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ClypseCoreException(string message)
        : base(message)
    {
    }
}