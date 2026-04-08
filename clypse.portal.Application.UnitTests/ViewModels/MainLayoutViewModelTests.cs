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
        this.mockPwaUpdateService.Setup(s => s.CheckForUpdateAsync()).ReturnsAsync(false);
        this.mockPwaUpdateService.Setup(s => s.InstallUpdateAsync()).ReturnsAsync(true);
        this.mockPwaUpdateService.Setup(s => s.ForceUpdateAsync()).ReturnsAsync(true);
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
        // Arrange / Act / Assert
        Assert.Throws<ArgumentNullException>(() => new MainLayoutViewModel(
            null!,
            this.appSettings,
            this.mockLogger.Object));
    }

    [Fact]
    public void GivenNullAppSettings_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Arrange / Act / Assert
        Assert.Throws<ArgumentNullException>(() => new MainLayoutViewModel(
            this.mockPwaUpdateService.Object,
            null!,
            this.mockLogger.Object));
    }

    [Fact]
    public void GivenNullLogger_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Arrange / Act / Assert
        Assert.Throws<ArgumentNullException>(() => new MainLayoutViewModel(
            this.mockPwaUpdateService.Object,
            this.appSettings,
            null!));
    }

    [Fact]
    public void GivenNewInstance_WhenCheckingInitialState_ThenDefaultValuesAreCorrect()
    {
        // Arrange
        var sut = CreateSut();

        // Assert
        Assert.False(sut.UpdateAvailable);
        Assert.False(sut.IsUpdating);
        Assert.False(sut.ShowChangesDialog);
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
    public void GivenAppSettings_WhenGetAppSettings_ThenReturnsCorrectSettings()
    {
        // Arrange
        var sut = CreateSut();

        // Assert
        Assert.Same(this.appSettings, sut.AppSettings);
    }

    [Fact]
    public async Task GivenFirstRender_WhenOnAfterRenderAsync_ThenSetupUpdateCallbacksIsCalled()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.OnAfterRenderAsync(firstRender: true);
        await Task.Delay(50);

        // Assert
        this.mockPwaUpdateService.Verify(s => s.SetupUpdateCallbacksAsync(), Times.Once);
    }

    [Fact]
    public async Task GivenNotFirstRender_WhenOnAfterRenderAsync_ThenSetupUpdateCallbacksIsNotCalled()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.OnAfterRenderAsync(firstRender: false);

        // Assert
        this.mockPwaUpdateService.Verify(s => s.SetupUpdateCallbacksAsync(), Times.Never);
    }

    [Fact]
    public async Task GivenSetupThrows_WhenOnAfterRenderAsync_ThenNoExceptionIsPropagated()
    {
        // Arrange
        var sut = CreateSut();
        this.mockPwaUpdateService.Setup(s => s.SetupUpdateCallbacksAsync())
            .ThrowsAsync(new Exception("setup failed"));

        // Act / Assert (no exception)
        await sut.OnAfterRenderAsync(firstRender: true);
    }

    [Fact]
    public async Task GivenUpdateNotAvailable_WhenHandleVersionClick_ThenChangesDialogIsShown()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.HandleVersionClickCommand.ExecuteAsync(null);

        // Assert
        Assert.True(sut.ShowChangesDialog);
    }

    [Fact]
    public async Task GivenUpdateAlreadyAvailable_WhenHandleVersionClick_ThenCheckForUpdateIsNotCalled()
    {
        // Arrange
        var sut = CreateSut();
        this.mockPwaUpdateService.Setup(s => s.IsUpdateAvailableAsync()).ReturnsAsync(true);
        await sut.OnAfterRenderAsync(firstRender: true);
        await Task.Delay(2500);

        // Act
        await sut.HandleVersionClickCommand.ExecuteAsync(null);
        await Task.Delay(50);

        // Assert
        Assert.True(sut.ShowChangesDialog);
        this.mockPwaUpdateService.Verify(s => s.CheckForUpdateAsync(), Times.Never);
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
    public async Task GivenInstallSucceeds_WhenHandleInstallUpdate_ThenInstallUpdateIsCalled()
    {
        // Arrange
        var sut = CreateSut();
        this.mockPwaUpdateService.Setup(s => s.InstallUpdateAsync()).ReturnsAsync(true);

        // Act
        await sut.HandleInstallUpdateCommand.ExecuteAsync(null);

        // Assert
        this.mockPwaUpdateService.Verify(s => s.InstallUpdateAsync(), Times.Once);
        Assert.False(sut.IsUpdating);
    }

    [Fact]
    public async Task GivenInstallFails_WhenHandleInstallUpdate_ThenForceUpdateIsCalled()
    {
        // Arrange
        var sut = CreateSut();
        this.mockPwaUpdateService.Setup(s => s.InstallUpdateAsync()).ReturnsAsync(false);

        // Act
        await sut.HandleInstallUpdateCommand.ExecuteAsync(null);

        // Assert
        this.mockPwaUpdateService.Verify(s => s.ForceUpdateAsync(), Times.Once);
        Assert.False(sut.IsUpdating);
    }

    [Fact]
    public async Task GivenInstallThrows_WhenHandleInstallUpdate_ThenIsUpdatingResetsToFalse()
    {
        // Arrange
        var sut = CreateSut();
        this.mockPwaUpdateService.Setup(s => s.InstallUpdateAsync())
            .ThrowsAsync(new Exception("install error"));

        // Act
        await sut.HandleInstallUpdateCommand.ExecuteAsync(null);

        // Assert
        Assert.False(sut.IsUpdating);
    }

    [Fact]
    public async Task GivenAlreadyUpdating_WhenHandleInstallUpdate_ThenInstallIsSkipped()
    {
        // Arrange
        var tcs = new TaskCompletionSource<bool>();
        this.mockPwaUpdateService.Setup(s => s.InstallUpdateAsync()).Returns(tcs.Task);
        var sut = CreateSut();

        var firstInstall = sut.HandleInstallUpdateCommand.ExecuteAsync(null);
        await Task.Delay(20);

        // Act
        await sut.HandleInstallUpdateCommand.ExecuteAsync(null);

        tcs.SetResult(true);
        await firstInstall;

        // Assert
        this.mockPwaUpdateService.Verify(s => s.InstallUpdateAsync(), Times.Once);
    }

    [Fact]
    public async Task GivenIsUpdateAvailableReturnsTrue_WhenRunUpdateLoop_ThenUpdateAvailableIsTrue()
    {
        // Arrange
        var sut = CreateSut();
        this.mockPwaUpdateService.Setup(s => s.IsUpdateAvailableAsync()).ReturnsAsync(true);

        // Act
        await sut.OnAfterRenderAsync(firstRender: true);
        await Task.Delay(2500);

        // Assert
        Assert.True(sut.UpdateAvailable);
    }

    [Fact]
    public async Task GivenIsUpdateAvailableThrows_WhenRunUpdateLoop_ThenNoExceptionPropagated()
    {
        // Arrange
        var sut = CreateSut();
        this.mockPwaUpdateService.Setup(s => s.IsUpdateAvailableAsync())
            .ThrowsAsync(new Exception("check failed"));

        // Act / Assert (no exception propagated out)
        await sut.OnAfterRenderAsync(firstRender: true);
        await Task.Delay(2500);
    }

    [Fact]
    public void GivenInstanceWithNoLoopStarted_WhenDisposed_ThenNoExceptionIsThrown()
    {
        // Arrange
        var sut = CreateSut();

        // Act / Assert
        sut.Dispose();
    }

    [Fact]
    public async Task GivenInstanceWithRunningLoop_WhenDisposed_ThenNoExceptionIsThrown()
    {
        // Arrange
        var sut = CreateSut();
        await sut.OnAfterRenderAsync(firstRender: true);

        // Act / Assert
        sut.Dispose();
    }

    [Fact]
    public async Task GivenDisposeAsyncThrows_WhenDisposed_ThenNoExceptionIsPropagated()
    {
        // Arrange
        var sut = CreateSut();
        await sut.OnAfterRenderAsync(firstRender: true);
        this.mockPwaUpdateService.Setup(s => s.DisposeAsync())
            .Returns(ValueTask.FromException(new Exception("dispose failed")));

        // Act / Assert
        sut.Dispose();
    }
}
