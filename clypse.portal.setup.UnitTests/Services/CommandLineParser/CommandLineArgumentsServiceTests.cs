using clypse.portal.setup.Services.CommandLineParser;
using System.Reflection;

namespace clypse.portal.setup.UnitTests.Services.CommandLineParser;

public class CommandLineArgumentsServiceTests
{
    [Theory]
    [InlineData(@"{currentexepath} hello -a=1 -b=2 -c=3", "hello -a=1 -b=2 -c=3")]
    public void GivenFullCommandLine_WhenGetArguments_ThenOnlyArgumentsReturned(
        string fullCommandLine,
        string expectedArguments)
    {
        // Arrange
        fullCommandLine = fullCommandLine.Replace("{currentexepath}", Assembly.GetEntryAssembly()!.Location);
        var sut = new CommandLineArgumentsService();

        // Act
        var arguments = sut.GetArguments(fullCommandLine);

        // Assert
        Assert.Equal(expectedArguments, arguments);
    }
}
