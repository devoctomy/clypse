using System.Runtime.CompilerServices;

namespace clypse.portal.Application.Helpers;

public static class ValidationHelpers
{
    public static T VerifiedAssignent<T>(
        T value,
        [CallerArgumentExpression(nameof(value))] string? parameterName = null)
    {
        var retVal = value ?? throw new ArgumentNullException(parameterName);
        return retVal;
    }
}
