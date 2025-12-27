using clypse.portal.setup.Services.CommandLineParser;

namespace clypse.portal.setup.UnitTests.Services.CommandLineParser;

public class HelpMessageFormatterTests
{
    [Theory]
    [InlineData(typeof(CommandLineTestOptions), "Data/CommandLineTestOptionsHelpMessage.txt")]
    [InlineData(typeof(CommandLineTestOptions3), "Data/CommandLineTestOptions3HelpMessage.txt")]
    public void GivenOptionsType_WhenFormat_ThenHelpMessageGenerated(
        Type optionsType,
        string expectedMessagePath)
    {
        // Arrange
        var expected = File.ReadAllText(expectedMessagePath);
        var sut = new HelpMessageFormatter();

        // Act
        var result = sut.Format(optionsType);

        // Assert
        expected = expected.Replace("\r\n", "\n");
        result = result.Replace("\r\n", "\n");
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GivenOptions_WhenGenericFormat_ThenHelpMessageGenerated()
    {
        // Arrange
        var expected = File.ReadAllText("Data/CommandLineTestOptionsHelpMessage.txt");
        var sut = new HelpMessageFormatter();

        // Act
        var result = sut.Format<CommandLineTestOptions>();

        // Assert
        expected = expected.Replace("\r\n", "\n");
        result = result.Replace("\r\n", "\n");
        Assert.Equal(expected, result);
    }
}
