using clypse.portal.setup.Enums;
using clypse.portal.setup.Services;
using clypse.portal.setup.Services.Orchestration;
using Microsoft.Extensions.Logging;
using Moq;

namespace clypse.portal.setup.UnitTests.Services;

public class SetupProgramTests
{
    [Fact]
    public async Task GivenProgram_WhenRunAsync_AndNoExceptions_ThenOrchstrationIsCalled_And0Returned()
    {
        // Arrange
        var options = new SetupOptions
        {
            BaseUrl = "https://example.com",
            AccessId = "test-access-id",
            InteractiveMode = false,
            EnableUpgradeMode = false
        };
        var mockSetupInteractiveMenuService = new Mock<ISetupInteractiveMenuService>();
        var mockClypseAwsSetupOrchestration = new Mock<IClypseAwsSetupOrchestration>();
        var sut = new SetupProgram(
            options,
            mockSetupInteractiveMenuService.Object,
            mockClypseAwsSetupOrchestration.Object,
            Mock.Of<ILogger<SetupProgram>>());

        // Act
        var result = await sut.RunAsync();

        // Assert
        Assert.Equal(0, result);

        mockClypseAwsSetupOrchestration.Verify(x => x.SetupClypseOnAwsAsync(
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

[Fact]
    public async Task GivenProgram_WhenRunAsync_AndPrepareFailss_ThenErrorCodeReturned()
    {
        // Arrange
        var options = new SetupOptions
        {
            BaseUrl = "https://example.com",
            AccessId = "test-access-id",
            InteractiveMode = false
        };
        var mockSetupInteractiveMenuService = new Mock<ISetupInteractiveMenuService>();
        var mockClypseAwsSetupOrchestration = new Mock<IClypseAwsSetupOrchestration>();
        var sut = new SetupProgram(
            options,
            mockSetupInteractiveMenuService.Object,
            mockClypseAwsSetupOrchestration.Object,
            Mock.Of<ILogger<SetupProgram>>());

        mockClypseAwsSetupOrchestration.Setup(x => x.SetupClypseOnAwsAsync(
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await sut.RunAsync();

        // Assert
        Assert.Equal(1, result);
    }    

    [Fact]
    public async Task GivenProgram_WhenRunAsync_AndExceptionOccurs_ThenOrchstrationIsCalled_AndErrorCodeReturned()
    {
        // Arrange
        var options = new SetupOptions
        {
            BaseUrl = "https://example.com",
            AccessId = "test-access-id",
            InteractiveMode = false
        };
        var mockSetupInteractiveMenuService = new Mock<ISetupInteractiveMenuService>();
        var mockClypseAwsSetupOrchestration = new Mock<IClypseAwsSetupOrchestration>();
        var sut = new SetupProgram(
            options,
            mockSetupInteractiveMenuService.Object,
            mockClypseAwsSetupOrchestration.Object,
            Mock.Of<ILogger<SetupProgram>>());

        mockClypseAwsSetupOrchestration.Setup(x => x.SetupClypseOnAwsAsync(
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await sut.RunAsync();

        // Assert
        Assert.Equal(1, result);

        mockClypseAwsSetupOrchestration.Verify(x => x.SetupClypseOnAwsAsync(
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
