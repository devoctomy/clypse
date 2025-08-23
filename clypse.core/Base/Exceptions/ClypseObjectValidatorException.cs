namespace clypse.core.Base.Exceptions;

public class ClypseObjectValidatorException : Exception
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
