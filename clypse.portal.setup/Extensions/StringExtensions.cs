namespace clypse.portal.setup.Extensions;

/// <summary>
/// Provides string helper extensions used by the setup tooling.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Masks all but the last N characters of the input string.
    /// </summary>
    /// <param name="input">The value to redact.</param>
    /// <param name="excludeLastNDigits">The number of trailing characters to leave visible.</param>
    /// <returns>The redacted string.</returns>
    public static string Redact(
        this string input,
        int excludeLastNDigits = 3)
    {
        if (string.IsNullOrEmpty(input) || input.Length <= excludeLastNDigits)
        {
            return new string('*', input?.Length ?? 0);
        }

        var redactedPortion = new string('*', input.Length - excludeLastNDigits);
        var visiblePortion = input[^excludeLastNDigits..];
        return redactedPortion + visiblePortion;
    }
}
