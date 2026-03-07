using clypse.portal.Application.Services.Interfaces;
using clypse.portal.Application.ViewModels;
using clypse.portal.Models.Settings;
using Microsoft.Extensions.Logging;
using Moq;

namespace clypse.portal.Application.UnitTests.ViewModels;

public class MainLayoutViewModelTests
{
    private readonly Mock<IPwaUpdateService> mockPwaUpdateService;
    private readonly AppSettings appSettings;
    private readonly Mock<ILogger<MainLayoutViewModel>> mockLogger;

    public MainLayoutViewModelTests()
    {
        this.mockPwaUpdateService = new Mock<IPwaUpdateService>();
        this.appSettings = new AppSettings();
        this.mockLogger = new Mock<ILogger<MainLayoutViewModel>>();

        this.mockPwaUpdateService.Setup(s => s.SetupUpdateCallbacksAsync()).Returns(Task.CompletedTask);
        this.mockPwaUpdateService.Setup(s => s.IsUpdateAvailableAsync()).ReturnsAsync(false);
        this.mockPwaUpdateService.Setup(s => s.DisposeAsync()).Returns(ValueTask.CompletedTask);
    }

    private MainLayoutViewModel CreateSut()
    {
        return new MainLayoutViewModel(
            this.mockPwaUpdateService.Object,
            this.appSettings,
            this.mockLogger.Object);
    }

    [Fact]
    public void GivenValidParameters_WhenConstructing_ThenCreatesInstance()
    {
        // Act
        var sut = CreateSut();

        // Assert
        Assert.NotNull(sut);
    }

    [Fact]
    public void GivenNullPwaUpdateService_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new MainLayoutViewModel(
            null!,
            this.appSettings,
            this.mockLogger.Object));
    }

    [Fact]
    public void GivenNullAppSettings_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new MainLayoutViewModel(
            this.mockPwaUpdateService.Object,
            null!,
            this.mockLogger.Object));
    }

    [Fact]
    public void GivenAppSettings_WhenGetAvailableVersion_ThenReturnsNonNullString()
    {
        // Arrange
        var sut = CreateSut();

        // Assert
        Assert.NotNull(sut.AvailableVersion);
    }

    [Fact]
    public void GivenNewInstance_WhenCheckingInitialState_ThenDefaultValuesAreCorrect()
    {
        // Arrange & Act
        var sut = CreateSut();

        // Assert
        Assert.False(sut.UpdateAvailable);
        Assert.False(sut.IsUpdating);
        Assert.False(sut.ShowChangesDialog);
    }

    [Fact]
    public async Task GivenUpdateAvailable_WhenHandleVersionClick_ThenChangesDialogIsShown()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.HandleVersionClickCommand.ExecuteAsync(null);

        // Assert
        Assert.True(sut.ShowChangesDialog);
    }

    [Fact]
    public void GivenOpenChangesDialog_WhenHandleCloseChangesDialog_ThenDialogIsClosed()
    {
        // Arrange
        var sut = CreateSut();
        sut.HandleVersionClickCommand.Execute(null);

        // Act
        sut.HandleCloseChangesDialogCommand.Execute(null);

        // Assert
        Assert.False(sut.ShowChangesDialog);
    }

    [Fact]
    public async Task GivenUpdateAvailable_WhenHandleInstallUpdate_ThenInstallUpdateIsCalled()
    {
        // Arrange
        var sut = CreateSut();
        this.mockPwaUpdateService.Setup(s => s.InstallUpdateAsync()).ReturnsAsync(true);

        // Act
        await sut.HandleInstallUpdateCommand.ExecuteAsync(null);

        // Assert
        this.mockPwaUpdateService.Verify(s => s.InstallUpdateAsync(), Times.Once);
    }

    [Fact]
    public void GivenAppSettings_WhenGetAppSettings_ThenReturnsCorrectSettings()
    {
        // Arrange
        var sut = CreateSut();

        // Assert
        Assert.Same(this.appSettings, sut.AppSettings);
    }
}
