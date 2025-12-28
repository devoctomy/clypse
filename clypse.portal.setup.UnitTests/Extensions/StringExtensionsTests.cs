using clypse.portal.setup.Extensions;

namespace clypse.portal.setup.UnitTests.Extensions;

public class StringExtensionsTests
{
    [Theory]
    [InlineData("1234567890", 3, "*******890")]
    [InlineData("abc123", 3, "***123")]
    [InlineData("test", 3, "*est")]
    [InlineData("12345", 2, "***45")]
    [InlineData("secret", 0, "******")]
    [InlineData("password123", 5, "******rd123")]
    [InlineData("a", 3, "*")]
    [InlineData("ab", 3, "**")]
    [InlineData("abc", 3, "***")]
    [InlineData("abcd", 3, "*bcd")]
    [InlineData("1234567890", 10, "**********")]
    [InlineData("key", 1, "**y")]
    public void GivenString_WhenRedact_ThenRedactsCorrectly(
        string input, 
        int excludeLastNDigits, 
        string expected)
    {
        // Act
        var result = input.Redact(excludeLastNDigits);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("1234567890", "*******890")]
    [InlineData("secretkey", "******key")]
    [InlineData("abc", "***")]
    [InlineData("test123", "****123")]
    public void GivenString_WhenRedactWithDefaultParameter_ThenRedactsWithLast3Visible(
        string input, 
        string expected)
    {
        // Act
        var result = input.Redact();

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null, 3, "")]
    [InlineData(null, 0, "")]
    [InlineData(null, 5, "")]
    public void GivenNullString_WhenRedact_ThenReturnsEmptyString(
        string? input, 
        int excludeLastNDigits, 
        string expected)
    {
        // Act
        var result = input!.Redact(excludeLastNDigits);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("", 0, "")]
    [InlineData("", 3, "")]
    [InlineData("", 10, "")]
    public void GivenEmptyString_WhenRedact_ThenReturnsEmptyString(
        string input, 
        int excludeLastNDigits, 
        string expected)
    {
        // Act
        var result = input.Redact(excludeLastNDigits);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("AWSAccessKey123456", 4, "**************3456")]
    [InlineData("sk-proj-1234567890abcdef", 8, "****************90abcdef")]
    [InlineData("ghp_1234567890123456789012345678901234", 6, "********************************901234")]
    public void GivenApiKey_WhenRedact_ThenRedactsSecretPortion(
        string input, 
        int excludeLastNDigits, 
        string expected)
    {
        // Act
        var result = input.Redact(excludeLastNDigits);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GivenLongString_WhenRedactWithZeroExclusion_ThenFullyRedacts()
    {
        // Arrange
        var input = "ThisIsAVeryLongSecretKeyThatShouldBeFullyRedacted";

        // Act
        var result = input.Redact(0);

        // Assert
        Assert.Equal(new string('*', input.Length), result);
        Assert.DoesNotContain(input, result);
    }

    [Fact]
    public void GivenString_WhenRedactMultipleTimes_ThenProducesSameResult()
    {
        // Arrange
        var input = "sensitiveData123";

        // Act
        var result1 = input.Redact(3);
        var result2 = input.Redact(3);

        // Assert
        Assert.Equal(result1, result2);
    }
}
