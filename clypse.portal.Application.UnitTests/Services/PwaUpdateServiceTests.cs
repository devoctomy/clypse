using clypse.portal.Application.Services;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Moq;

namespace clypse.portal.Application.UnitTests.Services;

public class PwaUpdateServiceTests
{
    private readonly Mock<IJSRuntime> mockJsRuntime;
    private readonly Mock<ILogger<PwaUpdateService>> mockLogger;

    public PwaUpdateServiceTests()
    {
        this.mockJsRuntime = new Mock<IJSRuntime>();
        this.mockLogger = new Mock<ILogger<PwaUpdateService>>();
    }

    private PwaUpdateService CreateSut()
    {
        return new PwaUpdateService(this.mockJsRuntime.Object, this.mockLogger.Object);
    }

    [Fact]
    public void GivenNullJSRuntime_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new PwaUpdateService(null!, this.mockLogger.Object));
        Assert.Equal("jsRuntime", exception.ParamName);
    }

    [Fact]
    public void GivenNullLogger_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new PwaUpdateService(this.mockJsRuntime.Object, null!));
        Assert.Equal("logger", exception.ParamName);
    }

    [Fact]
    public void GivenValidParameters_WhenConstructing_ThenCreatesInstance()
    {
        // Act
        var sut = this.CreateSut();

        // Assert
        Assert.NotNull(sut);
    }

    [Fact]
    public async Task GivenUpdateAvailable_WhenIsUpdateAvailableAsync_ThenReturnsTrue()
    {
        // Arrange
        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<bool>("PWAUpdateService.isUpdateAvailable", It.IsAny<object?[]>()))
            .ReturnsAsync(true);

        var sut = this.CreateSut();

        // Act
        var result = await sut.IsUpdateAvailableAsync();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task GivenNoUpdateAvailable_WhenIsUpdateAvailableAsync_ThenReturnsFalse()
    {
        // Arrange
        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<bool>("PWAUpdateService.isUpdateAvailable", It.IsAny<object?[]>()))
            .ReturnsAsync(false);

        var sut = this.CreateSut();

        // Act
        var result = await sut.IsUpdateAvailableAsync();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GivenJsException_WhenIsUpdateAvailableAsync_ThenReturnsFalse()
    {
        // Arrange
        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<bool>("PWAUpdateService.isUpdateAvailable", It.IsAny<object?[]>()))
            .ThrowsAsync(new JSException("JS error"));

        var sut = this.CreateSut();

        // Act
        var result = await sut.IsUpdateAvailableAsync();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GivenUpdateCheck_WhenCheckForUpdateAsync_ThenInvokesJavaScript()
    {
        // Arrange
        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<bool>("PWAUpdateService.checkForUpdate", It.IsAny<object?[]>()))
            .ReturnsAsync(true);

        var sut = this.CreateSut();

        // Act
        var result = await sut.CheckForUpdateAsync();

        // Assert
        Assert.True(result);
        this.mockJsRuntime.Verify(
            x => x.InvokeAsync<bool>("PWAUpdateService.checkForUpdate", It.IsAny<object?[]>()),
            Times.Once);
    }

    [Fact]
    public async Task GivenJsException_WhenCheckForUpdateAsync_ThenReturnsFalse()
    {
        // Arrange
        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<bool>("PWAUpdateService.checkForUpdate", It.IsAny<object?[]>()))
            .ThrowsAsync(new JSException("JS error"));

        var sut = this.CreateSut();

        // Act
        var result = await sut.CheckForUpdateAsync();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GivenUpdateWaiting_WhenInstallUpdateAsync_ThenInvokesJavaScript()
    {
        // Arrange
        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<bool>("PWAUpdateService.installUpdate", It.IsAny<object?[]>()))
            .ReturnsAsync(true);

        var sut = this.CreateSut();

        // Act
        var result = await sut.InstallUpdateAsync();

        // Assert
        Assert.True(result);
        this.mockJsRuntime.Verify(
            x => x.InvokeAsync<bool>("PWAUpdateService.installUpdate", It.IsAny<object?[]>()),
            Times.Once);
    }

    [Fact]
    public async Task GivenJsException_WhenInstallUpdateAsync_ThenReturnsFalse()
    {
        // Arrange
        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<bool>("PWAUpdateService.installUpdate", It.IsAny<object?[]>()))
            .ThrowsAsync(new JSException("JS error"));

        var sut = this.CreateSut();

        // Act
        var result = await sut.InstallUpdateAsync();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GivenForceUpdate_WhenForceUpdateAsync_ThenInvokesJavaScript()
    {
        // Arrange
        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<bool>("PWAUpdateService.forceUpdate", It.IsAny<object?[]>()))
            .ReturnsAsync(true);

        var sut = this.CreateSut();

        // Act
        var result = await sut.ForceUpdateAsync();

        // Assert
        Assert.True(result);
        this.mockJsRuntime.Verify(
            x => x.InvokeAsync<bool>("PWAUpdateService.forceUpdate", It.IsAny<object?[]>()),
            Times.Once);
    }

    [Fact]
    public async Task GivenJsException_WhenForceUpdateAsync_ThenReturnsFalse()
    {
        // Arrange
        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<bool>("PWAUpdateService.forceUpdate", It.IsAny<object?[]>()))
            .ThrowsAsync(new JSException("JS error"));

        var sut = this.CreateSut();

        // Act
        var result = await sut.ForceUpdateAsync();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GivenCallbacks_WhenSetupUpdateCallbacksAsync_ThenCompletesSuccessfully()
    {
        // Arrange
        var sut = this.CreateSut();
        string? errorReceived = null;

        Func<Task> onAvailable = () => Task.CompletedTask;
        Func<Task> onInstalled = () => Task.CompletedTask;
        Func<string, Task> onError = e => { errorReceived = e; return Task.CompletedTask; };

        // Act & Assert
        var exception = await Record.ExceptionAsync(() =>
            sut.SetupUpdateCallbacksAsync(onAvailable, onInstalled, onError));

        Assert.Null(exception);
    }

    [Fact]
    public async Task GivenNullCallbacks_WhenSetupUpdateCallbacksAsync_ThenCompletesSuccessfully()
    {
        // Arrange
        var sut = this.CreateSut();

        // Act & Assert
        var exception = await Record.ExceptionAsync(() =>
            sut.SetupUpdateCallbacksAsync(null, null, null));

        Assert.Null(exception);
    }

    [Fact]
    public async Task GivenService_WhenDisposeAsync_ThenCompletesSuccessfully()
    {
        // Arrange
        var sut = this.CreateSut();

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () => await sut.DisposeAsync());
        Assert.Null(exception);
    }
}
