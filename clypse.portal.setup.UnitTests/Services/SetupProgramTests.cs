using clypse.portal.setup.Services;
using clypse.portal.setup.Services.CommandLineParser;
using clypse.portal.setup.Services.Orchestration;
using Microsoft.Extensions.Logging;
using Moq;

namespace clypse.portal.setup.UnitTests.Services;

public class SetupProgramTests
{
    [Fact]
    public async Task GivenProgram_WhenRun_AndNoExceptions_ThenOrchstrationIsCalled_And0Returned()
    {
        // Arrange
        var mockClypseAwsSetupOrchestration = new Mock<IClypseAwsSetupOrchestration>();
        var mockCommandLineArgumentsService = new Mock<ICommandLineArgumentsService>();
        var mockCommandLineParserService = new Mock<ICommandLineParserService>();
        var sut = new SetupProgram(
            mockClypseAwsSetupOrchestration.Object,
            mockCommandLineArgumentsService.Object,
            mockCommandLineParserService.Object,
            Mock.Of<ILogger<SetupProgram>>());

        var arguments = "--someArgument value";

        mockCommandLineArgumentsService.Setup(x => x.GetArguments(
            It.IsAny<string>()))
            .Returns(arguments);

        // Act
        var result = await sut.Run();

        // Assert
        Assert.Equal(0, result);

        mockCommandLineArgumentsService.Verify(x => x.GetArguments(
            It.IsAny<string>()),
            Times.Once);

        mockClypseAwsSetupOrchestration.Verify(x => x.SetupClypseOnAwsAsync(
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GivenProgram_WhenRun_AndExceptionOccurs_ThenOrchstrationIsCalled_AndErrorCodeReturned()
    {
        // Arrange
        var mockClypseAwsSetupOrchestration = new Mock<IClypseAwsSetupOrchestration>();
        var mockCommandLineArgumentsService = new Mock<ICommandLineArgumentsService>();
        var mockCommandLineParserService = new Mock<ICommandLineParserService>();
        var sut = new SetupProgram(
            mockClypseAwsSetupOrchestration.Object,
            mockCommandLineArgumentsService.Object,
            mockCommandLineParserService.Object,
            Mock.Of<ILogger<SetupProgram>>());

        var arguments = "--someArgument value";

        mockCommandLineArgumentsService.Setup(x => x.GetArguments(
            It.IsAny<string>()))
            .Returns(arguments);

        mockClypseAwsSetupOrchestration.Setup(x => x.SetupClypseOnAwsAsync(
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await sut.Run();

        // Assert
        Assert.Equal(1, result);

        mockCommandLineArgumentsService.Verify(x => x.GetArguments(
            It.IsAny<string>()),
            Times.Once);

        mockClypseAwsSetupOrchestration.Verify(x => x.SetupClypseOnAwsAsync(
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
