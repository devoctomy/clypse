using Devoctomy.Blazwiz.Core.Exceptions;

namespace Clypse.Portal.Application.Exceptions;

/// <summary>
/// An error clypse reports to the user. Its code (see <see cref="ClypseErrorCodes"/>) names its text, so it is shown in
/// the user's language through the framework's error text.
/// </summary>
public class ClypseException : BlazwizException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClypseException"/> class.
    /// </summary>
    /// <param name="code">The error's code, from <see cref="ClypseErrorCodes"/>.</param>
    /// <param name="arguments">Values for the text's placeholders.</param>
    public ClypseException(string code, params object[] arguments)
        : base(code, arguments)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ClypseException"/> class, with the error that caused it.
    /// </summary>
    /// <param name="code">The error's code, from <see cref="ClypseErrorCodes"/>.</param>
    /// <param name="innerException">The error that caused it.</param>
    /// <param name="arguments">Values for the text's placeholders.</param>
    public ClypseException(string code, Exception? innerException, params object[] arguments)
        : base(code, innerException, arguments)
    {
    }
}
