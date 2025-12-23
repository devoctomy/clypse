namespace clypse.portal.setup.Extensions;

public static class StringExtensions
{
    public static string Redact(
        this string input,
        int excludeLastNDigits = 3)
    {
        if (string.IsNullOrEmpty(input) || input.Length <= excludeLastNDigits)
        {
            return new string('*', input?.Length ?? 0);
        }

        var redactedPortion = new string('*', input.Length - excludeLastNDigits);
        var visiblePortion = input.Substring(input.Length - excludeLastNDigits);
        return redactedPortion + visiblePortion;
    }
}
