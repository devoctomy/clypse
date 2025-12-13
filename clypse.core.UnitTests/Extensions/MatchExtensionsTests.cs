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
}
