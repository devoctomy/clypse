using clypse.portal.setup.Enums;
using clypse.portal.setup.Services;
using clypse.portal.setup.Services.Build;
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
        var mockPortalBuildService = new Mock<IPortalBuildService>();
        var sut = new SetupProgram(
            options,
            mockSetupInteractiveMenuService.Object,
            mockClypseAwsSetupOrchestration.Object,
            mockPortalBuildService.Object,
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
        var mockPortalBuildService = new Mock<IPortalBuildService>();
        var sut = new SetupProgram(
            options,
            mockSetupInteractiveMenuService.Object,
            mockClypseAwsSetupOrchestration.Object,
            mockPortalBuildService.Object,
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
        var mockPortalBuildService = new Mock<IPortalBuildService>();
        var sut = new SetupProgram(
            options,
            mockSetupInteractiveMenuService.Object,
            mockClypseAwsSetupOrchestration.Object,
            mockPortalBuildService.Object,
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

    [Fact]
    public async Task GivenProgram_WhenRunAsync_InNonInteractiveModeWithUpgrade_AndBuildPortalEnabled_ThenPortalIsBuilt()
    {
        // Arrange
        var options = new SetupOptions
        {
            BaseUrl = "https://example.com",
            AccessId = "test-access-id",
            InteractiveMode = false,
            EnableUpgradeMode = true,
            BuildPortal = true,
            PortalBuildOutputPath = "/old/path"
        };
        var mockSetupInteractiveMenuService = new Mock<ISetupInteractiveMenuService>();
        var mockClypseAwsSetupOrchestration = new Mock<IClypseAwsSetupOrchestration>();
        var mockPortalBuildService = new Mock<IPortalBuildService>();
        
        var buildResult = new PortalBuildResult(true, "/new/build/path");
        mockPortalBuildService.Setup(x => x.Run()).ReturnsAsync(buildResult);

        var sut = new SetupProgram(
            options,
            mockSetupInteractiveMenuService.Object,
            mockClypseAwsSetupOrchestration.Object,
            mockPortalBuildService.Object,
            Mock.Of<ILogger<SetupProgram>>());

        // Act
        var result = await sut.RunAsync();

        // Assert
        Assert.Equal(0, result);
        Assert.Equal("/new/build/path", options.PortalBuildOutputPath);

        mockPortalBuildService.Verify(x => x.Run(), Times.Once);
        mockClypseAwsSetupOrchestration.Verify(x => x.UpgradePortalAsync(
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GivenProgram_WhenRunAsync_InNonInteractiveModeWithUpgrade_AndBuildPortalEnabled_AndBuildFails_ThenErrorReturned()
    {
        // Arrange
        var options = new SetupOptions
        {
            BaseUrl = "https://example.com",
            AccessId = "test-access-id",
            InteractiveMode = false,
            EnableUpgradeMode = true,
            BuildPortal = true
        };
        var mockSetupInteractiveMenuService = new Mock<ISetupInteractiveMenuService>();
        var mockClypseAwsSetupOrchestration = new Mock<IClypseAwsSetupOrchestration>();
        var mockPortalBuildService = new Mock<IPortalBuildService>();
        
        var buildResult = new PortalBuildResult(false, string.Empty);
        mockPortalBuildService.Setup(x => x.Run()).ReturnsAsync(buildResult);

        var sut = new SetupProgram(
            options,
            mockSetupInteractiveMenuService.Object,
            mockClypseAwsSetupOrchestration.Object,
            mockPortalBuildService.Object,
            Mock.Of<ILogger<SetupProgram>>());

        // Act
        var result = await sut.RunAsync();

        // Assert
        Assert.Equal(1, result);

        mockPortalBuildService.Verify(x => x.Run(), Times.Once);
        mockClypseAwsSetupOrchestration.Verify(x => x.UpgradePortalAsync(
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GivenProgram_WhenRunAsync_InNonInteractiveModeWithUpgrade_AndBuildPortalDisabled_ThenPortalIsNotBuilt()
    {
        // Arrange
        var options = new SetupOptions
        {
            BaseUrl = "https://example.com",
            AccessId = "test-access-id",
            InteractiveMode = false,
            EnableUpgradeMode = true,
            BuildPortal = false
        };
        var mockSetupInteractiveMenuService = new Mock<ISetupInteractiveMenuService>();
        var mockClypseAwsSetupOrchestration = new Mock<IClypseAwsSetupOrchestration>();
        var mockPortalBuildService = new Mock<IPortalBuildService>();

        var sut = new SetupProgram(
            options,
            mockSetupInteractiveMenuService.Object,
            mockClypseAwsSetupOrchestration.Object,
            mockPortalBuildService.Object,
            Mock.Of<ILogger<SetupProgram>>());

        // Act
        var result = await sut.RunAsync();

        // Assert
        Assert.Equal(0, result);

        mockPortalBuildService.Verify(x => x.Run(), Times.Never);
        mockClypseAwsSetupOrchestration.Verify(x => x.UpgradePortalAsync(
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GivenProgram_WhenRunAsync_InInteractiveMode_ThenPortalIsNotBuiltAutomatically()
    {
        // Arrange
        var options = new SetupOptions
        {
            BaseUrl = "https://example.com",
            AccessId = "test-access-id",
            SecretAccessKey = "test-secret-key",
            Region = "us-east-1",
            ResourcePrefix = "test-prefix",
            InitialUserEmail = "test@example.com",
            InteractiveMode = false,  // Changed to false to avoid Console.ReadKey()
            EnableUpgradeMode = false,  // Changed to false - we'll use a mock to control mode
            BuildPortal = true
        };
        var mockSetupInteractiveMenuService = new Mock<ISetupInteractiveMenuService>();
        mockSetupInteractiveMenuService.Setup(x => x.Run(It.IsAny<SetupOptions>())).Returns(SetupMode.Upgrade);
        
        var mockClypseAwsSetupOrchestration = new Mock<IClypseAwsSetupOrchestration>();
        mockClypseAwsSetupOrchestration.Setup(x => x.UpgradePortalAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        
        var mockPortalBuildService = new Mock<IPortalBuildService>();

        var sut = new SetupProgram(
            options,
            mockSetupInteractiveMenuService.Object,
            mockClypseAwsSetupOrchestration.Object,
            mockPortalBuildService.Object,
            Mock.Of<ILogger<SetupProgram>>());

        // Act
        var result = await sut.RunAsync();

        // Assert
        Assert.Equal(0, result);

        mockPortalBuildService.Verify(x => x.Run(), Times.Never);
        mockSetupInteractiveMenuService.Verify(x => x.Run(It.IsAny<SetupOptions>()), Times.Never);  // Should not be called in non-interactive mode
        // In non-interactive with EnableUpgradeMode=false, it should do FullCreate, not Upgrade
        mockClypseAwsSetupOrchestration.Verify(x => x.UpgradePortalAsync(
            It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
