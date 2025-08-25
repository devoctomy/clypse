using clypse.core.Exceptions;

namespace clypse.core.Base.Exceptions;

public class ClypseObjectValidatorException : ClypseCoreException
{
    public List<string> MissingProperties { get; }

    public ClypseObjectValidatorException(
        string message,
        List<string> missingProperties) :
        base(message)
    {
        MissingProperties = missingProperties;
    }
}
