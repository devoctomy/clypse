using clypse.portal.Application.Services;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using Moq;

namespace clypse.portal.Application.UnitTests.Services;

public class LocalStorageServiceTests
{
    private readonly Mock<IJSRuntime> mockJsRuntime;

    public LocalStorageServiceTests()
    {
        this.mockJsRuntime = new Mock<IJSRuntime>();
    }

    private LocalStorageService CreateSut()
    {
        return new LocalStorageService(this.mockJsRuntime.Object);
    }

    [Fact]
    public void GivenNullJSRuntime_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new LocalStorageService(null!));
        Assert.Equal("jsRuntime", exception.ParamName);
    }

    [Fact]
    public void GivenValidJSRuntime_WhenConstructing_ThenCreatesInstance()
    {
        // Act
        var sut = this.CreateSut();

        // Assert
        Assert.NotNull(sut);
    }

    [Fact]
    public async Task GivenKey_WhenGetItemAsync_ThenInvokesJavaScriptGetItem()
    {
        // Arrange
        var key = "test-key";
        var expectedValue = "test-value";
        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<string?>("localStorage.getItem", It.Is<object?[]>(args => args.Length == 1 && (string?)args[0] == key)))
            .ReturnsAsync(expectedValue);

        var sut = this.CreateSut();

        // Act
        var result = await sut.GetItemAsync(key);

        // Assert
        Assert.Equal(expectedValue, result);
        this.mockJsRuntime.Verify(
            x => x.InvokeAsync<string?>("localStorage.getItem", It.Is<object?[]>(args => args.Length == 1 && (string?)args[0] == key)),
            Times.Once);
    }

    [Fact]
    public async Task GivenNonExistentKey_WhenGetItemAsync_ThenReturnsNull()
    {
        // Arrange
        var key = "missing-key";
        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<string?>("localStorage.getItem", It.IsAny<object?[]>()))
            .ReturnsAsync((string?)null);

        var sut = this.CreateSut();

        // Act
        var result = await sut.GetItemAsync(key);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GivenKeyAndValue_WhenSetItemAsync_ThenInvokesJavaScriptSetItem()
    {
        // Arrange
        var key = "test-key";
        var value = "test-value";
        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<IJSVoidResult>("localStorage.setItem", It.IsAny<object?[]>()))
            .ReturnsAsync(Mock.Of<IJSVoidResult>());

        var sut = this.CreateSut();

        // Act
        await sut.SetItemAsync(key, value);

        // Assert
        this.mockJsRuntime.Verify(
            x => x.InvokeAsync<IJSVoidResult>("localStorage.setItem", It.Is<object?[]>(args => args.Length == 2 && (string?)args[0] == key && (string?)args[1] == value)),
            Times.Once);
    }

    [Fact]
    public async Task GivenKey_WhenRemoveItemAsync_ThenInvokesJavaScriptRemoveItem()
    {
        // Arrange
        var key = "test-key";
        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<IJSVoidResult>("localStorage.removeItem", It.IsAny<object?[]>()))
            .ReturnsAsync(Mock.Of<IJSVoidResult>());

        var sut = this.CreateSut();

        // Act
        await sut.RemoveItemAsync(key);

        // Assert
        this.mockJsRuntime.Verify(
            x => x.InvokeAsync<IJSVoidResult>("localStorage.removeItem", It.Is<object?[]>(args => args.Length == 1 && (string?)args[0] == key)),
            Times.Once);
    }

    [Fact]
    public async Task GivenLocalStorageWithItems_WhenClearAllExceptPersistentSettingsAsync_ThenInvokesEvalWithScript()
    {
        // Arrange
        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<IJSVoidResult>("eval", It.IsAny<object?[]>()))
            .ReturnsAsync(Mock.Of<IJSVoidResult>());

        var sut = this.CreateSut();

        // Act
        await sut.ClearAllExceptPersistentSettingsAsync();

        // Assert
        this.mockJsRuntime.Verify(
            x => x.InvokeAsync<IJSVoidResult>("eval", It.Is<object?[]>(args => args.Length == 1 && ((string?)args[0])!.Contains("localStorage"))),
            Times.Once);
    }

    [Fact]
    public async Task GivenClearAll_WhenClearAllExceptPersistentSettingsAsync_ThenScriptPreservesUserAndSettingsKeys()
    {
        // Arrange
        string? capturedScript = null;
        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<IJSVoidResult>("eval", It.IsAny<object?[]>()))
            .Callback<string, object?[]>((_, args) => capturedScript = args[0] as string)
            .ReturnsAsync(Mock.Of<IJSVoidResult>());

        var sut = this.CreateSut();

        // Act
        await sut.ClearAllExceptPersistentSettingsAsync();

        // Assert
        Assert.NotNull(capturedScript);
        Assert.Contains("users", capturedScript);
        Assert.Contains("clypse_user_settings", capturedScript);
    }
}
