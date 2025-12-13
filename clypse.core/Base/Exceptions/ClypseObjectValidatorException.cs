using clypse.core.Exceptions;

namespace clypse.core.Base.Exceptions;

/// <summary>
/// ClypseObjectValidatorException.
/// </summary>
public class ClypseObjectValidatorException : ClypseCoreException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClypseObjectValidatorException"/> class.
    /// </summary>
    /// <param name="message">Message text of the exception.</param>
    /// <param name="missingProperties">List of missing properties that caused the validation exception.</param>
    public ClypseObjectValidatorException(
        string message,
        List<string> missingProperties)
        : base(message)
    {
        this.MissingProperties = missingProperties;
    }

    /// <summary>
    /// Gets a List of missing properties relating to this exception.
    /// </summary>
    public List<string> MissingProperties { get; }
}
