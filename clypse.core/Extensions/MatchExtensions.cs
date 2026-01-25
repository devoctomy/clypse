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
        ValidateArguments(match, swapMatch, matchString);

        var (primaryIndex, primaryLength, primaryEnd) = (match.Index, match.Length, match.Index + match.Length);
        var (secondaryIndex, secondaryLength, secondaryEnd) = (swapMatch.Index, swapMatch.Length, swapMatch.Index + swapMatch.Length);

        ValidateRanges(matchString, primaryIndex, primaryLength, primaryEnd, secondaryIndex, secondaryLength, secondaryEnd);
        ValidateSegments(matchString, match, swapMatch, primaryIndex, primaryLength, secondaryIndex, secondaryLength);
        ValidateNonOverlap(primaryEnd, secondaryIndex, secondaryEnd, primaryIndex);

        var first = (primaryIndex <= secondaryIndex) ? match : swapMatch;
        var second = (primaryIndex <= secondaryIndex) ? swapMatch : match;

        int firstIndex = first.Index, firstLength = first.Length, firstEnd = firstIndex + firstLength;
        int secondIndex = second.Index, secondLength = second.Length, secondEnd = secondIndex + secondLength;

        return string.Concat(
            matchString.AsSpan(0, firstIndex).ToString(),
            matchString.AsSpan(secondIndex, secondLength).ToString(),
            matchString.AsSpan(firstEnd, secondIndex - firstEnd).ToString(),
            matchString.AsSpan(firstIndex, firstLength).ToString(),
            matchString.AsSpan(secondEnd, matchString.Length - secondEnd).ToString());
    }

    private static void ValidateArguments(
        Match match,
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
    }

    private static void ValidateRanges(
        string matchString,
        int primaryIndex,
        int primaryLength,
        int primaryEnd,
        int secondaryIndex,
        int secondaryLength,
        int secondaryEnd)
    {
        if (primaryIndex < 0 || primaryLength < 0 || primaryEnd > matchString.Length
            || secondaryIndex < 0 || secondaryLength < 0 || secondaryEnd > matchString.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(matchString), "Match indices are out of range for matchString.");
        }
    }

    private static void ValidateSegments(
        string matchString,
        Match match,
        Match swapMatch,
        int primaryIndex,
        int primaryLength,
        int secondaryIndex,
        int secondaryLength)
    {
        var matchStringSegment1 = matchString.AsSpan(primaryIndex, primaryLength).ToString();
        var matchStringSegment2 = matchString.AsSpan(secondaryIndex, secondaryLength).ToString();
        var segmentMatches = string.Equals(matchStringSegment1, match.Value, StringComparison.Ordinal)
                             && string.Equals(matchStringSegment2, swapMatch.Value, StringComparison.Ordinal);

        if (!segmentMatches)
        {
            throw new ArgumentException("Provided matches do not correspond to matchString at their indices.");
        }
    }

    private static void ValidateNonOverlap(
        int primaryEnd,
        int secondaryIndex,
        int secondaryEnd,
        int primaryIndex)
    {
        if (!(primaryEnd <= secondaryIndex || secondaryEnd <= primaryIndex))
        {
            throw new InvalidOperationException("Matches overlap; cannot swap.");
        }
    }
}
