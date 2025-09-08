using System.Text.RegularExpressions;

namespace clypse.core.Extensions;

/// <summary>
/// Extension methods for the <see cref="Match"/> class.
/// </summary>
public static class MatchExtensions
{
    /// <summary>
    /// Swaps the values of two non-overlapping matches within the given string.
    /// </summary>
    /// <param name="match">The first match to swap.</param>
    /// <param name="swapMatch">The second match to swap.</param>
    /// <param name="matchString">The original string containing the matches.</param>
    /// <returns>A new string with the values of the two matches swapped.</returns>
    /// <exception cref="ArgumentNullException">Thrown if any argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown if either match is not successful or does not correspond to the matchString.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if match indices are out of range for matchString.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the matches overlap.</exception>
    public static string SwapWith(
        this Match match,
        Match swapMatch,
        string matchString)
    {
        ArgumentNullException.ThrowIfNull(match);
        ArgumentNullException.ThrowIfNull(swapMatch);
        ArgumentNullException.ThrowIfNull(matchString);

        if (!match.Success)
        {
            throw new ArgumentException("Primary match is not successful.", nameof(match));
        }

        if (!swapMatch.Success)
        {
            throw new ArgumentException("swapMatch is not successful.", nameof(swapMatch));
        }

        int i1 = match.Index, l1 = match.Length, e1 = i1 + l1;
        int i2 = swapMatch.Index, l2 = swapMatch.Length, e2 = i2 + l2;

        if (i1 < 0 || l1 < 0 || e1 > matchString.Length ||
            i2 < 0 || l2 < 0 || e2 > matchString.Length)
        {
            throw new ArgumentOutOfRangeException("Match indices are out of range for matchString.");
        }

        if (!string.Equals(matchString.AsSpan(i1, l1).ToString(), match.Value.ToString(), StringComparison.Ordinal) ||
            !string.Equals(matchString.AsSpan(i2, l2).ToString(), swapMatch.Value.ToString(), StringComparison.Ordinal))
        {
            throw new ArgumentException("Provided matches do not correspond to matchString at their indices.");
        }

        if (!(e1 <= i2 || e2 <= i1))
        {
            throw new InvalidOperationException("Matches overlap; cannot swap.");
        }

        var first = (i1 <= i2) ? match : swapMatch;
        var second = (i1 <= i2) ? swapMatch : match;

        int fI = first.Index, fL = first.Length, fE = fI + fL;
        int sI = second.Index, sL = second.Length, sE = sI + sL;

        return string.Concat(
            matchString.AsSpan(0, fI).ToString(),
            matchString.AsSpan(sI, sL).ToString(),
            matchString.AsSpan(fE, sI - fE).ToString(),
            matchString.AsSpan(fI, fL).ToString(),
            matchString.AsSpan(sE, matchString.Length - sE).ToString());
    }
}
