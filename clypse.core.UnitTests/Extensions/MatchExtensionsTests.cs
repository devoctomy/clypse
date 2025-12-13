using System.Text.RegularExpressions;
using clypse.core.Extensions;

namespace clypse.core.UnitTests.Extensions;

public class MatchExtensionsTests
{
    [Theory]
    [InlineData("The quick brown fox jumps over the lazy dog.", @"\b\w{3}\b", 1, 2, "The quick brown the jumps over fox lazy dog.")]
    [InlineData("This {pop} is a {thing I am testing}, and {stuff,2} I {stuff,4} it works.", @"\{[^}]+\}", 1, 3, "This {pop} is a {stuff,4}, and {stuff,2} I {thing I am testing} it works.")]
    public void GivenRegexMatches_AndString_WhenSwapWith_ThenReturnsSwappedString(
        string input,
        string regexPattern,
        int match1Index,
        int match2Index,
        string expectedResult)
    {
        // Arrange
        var regex = new Regex(regexPattern);
        var matches = regex.Matches(input);
        var match1 = matches[match1Index];
        var match2 = matches[match2Index];

        // Act
        var result = match1.SwapWith(match2, input);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public void GivenUnsuccessfulMatch_WhenSwapWith_ThenThrowsArgumentException()
    {
        // Arrange
        var input = "foo bar";
        var successfulMatch = new Regex("foo").Match(input);
        var unsuccessfulMatch = Regex.Match(input, "baz");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => unsuccessfulMatch.SwapWith(successfulMatch, input));
    }

    [Fact]
    public void GivenNullMatch_WhenSwapWith_ThenThrowsArgumentNullException()
    {
        // Arrange
        var input = "foo";
        var swapMatch = Regex.Match(input, "foo");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((Match)null!).SwapWith(swapMatch, input));
    }

    [Fact]
    public void GivenUnsuccessfulSwapMatch_WhenSwapWith_ThenThrowsArgumentException()
    {
        // Arrange
        var input = "foo bar";
        var primaryMatch = Regex.Match(input, "foo");
        var unsuccessfulSwapMatch = Regex.Match(input, "baz");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => primaryMatch.SwapWith(unsuccessfulSwapMatch, input));
    }

    [Fact]
    public void GivenMatchesNotAlignedToMatchString_WhenSwapWith_ThenThrowsArgumentException()
    {
        // Arrange
        var matchSource = "foo bar";
        var providedMatchString = "baz car";
        var match1 = Regex.Match(matchSource, "foo");
        var match2 = Regex.Match(matchSource, "bar");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => match1.SwapWith(match2, providedMatchString));
    }

    [Fact]
    public void GivenOutOfRangeMatchIndices_WhenSwapWith_ThenThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var source = "foobar";
        var shorter = "foo";
        var match1 = Regex.Match(source, "foo");
        var match2 = Regex.Match(source, "bar");

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => match1.SwapWith(match2, shorter));
    }

    [Fact]
    public void GivenOverlappingMatches_WhenSwapWith_ThenThrowsInvalidOperationException()
    {
        // Arrange
        var input = "abcde";
        var match1 = Regex.Match(input, "abc");
        var match2 = Regex.Match(input, "bcd");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => match1.SwapWith(match2, input));
    }
}
