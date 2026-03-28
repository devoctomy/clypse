using System.Text.Json;
using clypse.portal.Application.Services;
using clypse.portal.Models.Settings;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using Moq;

namespace clypse.portal.Application.UnitTests.Services;

public class UserSettingsServiceTests
{
    private const string SettingsKey = "clypse_user_settings";
    private const string LegacyThemeKey = "clypse_theme";

    private readonly Mock<IJSRuntime> mockJsRuntime;

    public UserSettingsServiceTests()
    {
        this.mockJsRuntime = new Mock<IJSRuntime>();
    }

    private UserSettingsService CreateSut()
    {
        return new UserSettingsService(this.mockJsRuntime.Object);
    }

    [Fact]
    public void GivenNullJSRuntime_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new UserSettingsService(null!));
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
    public async Task GivenStoredSettings_WhenGetSettingsAsync_ThenReturnsDeserializedSettings()
    {
        // Arrange
        var expected = new UserSettings { Theme = "dark" };
        var json = JsonSerializer.Serialize(expected);
        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<string?>("localStorage.getItem", It.Is<object?[]>(args => (string?)args[0] == SettingsKey)))
            .ReturnsAsync(json);

        var sut = this.CreateSut();

        // Act
        var result = await sut.GetSettingsAsync();

        // Assert
        Assert.Equal("dark", result.Theme);
    }

    [Fact]
    public async Task GivenNoSettings_WhenGetSettingsAsync_ThenReturnsDefaultSettings()
    {
        // Arrange
        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<string?>("localStorage.getItem", It.IsAny<object?[]>()))
            .ReturnsAsync((string?)null);

        var sut = this.CreateSut();

        // Act
        var result = await sut.GetSettingsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("light", result.Theme);
    }

    [Fact]
    public async Task GivenLegacyThemeSetting_WhenGetSettingsAsync_ThenMigratesAndReturnsTheme()
    {
        // Arrange
        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<string?>("localStorage.getItem", It.Is<object?[]>(args => (string?)args[0] == SettingsKey)))
            .ReturnsAsync((string?)null);
        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<string?>("localStorage.getItem", It.Is<object?[]>(args => (string?)args[0] == LegacyThemeKey)))
            .ReturnsAsync("dark");
        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<IJSVoidResult>("localStorage.setItem", It.IsAny<object?[]>()))
            .ReturnsAsync(Mock.Of<IJSVoidResult>());
        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<IJSVoidResult>("localStorage.removeItem", It.IsAny<object?[]>()))
            .ReturnsAsync(Mock.Of<IJSVoidResult>());

        var sut = this.CreateSut();

        // Act
        var result = await sut.GetSettingsAsync();

        // Assert
        Assert.Equal("dark", result.Theme);
        this.mockJsRuntime.Verify(
            x => x.InvokeAsync<IJSVoidResult>("localStorage.removeItem", It.Is<object?[]>(args => (string?)args[0] == LegacyThemeKey)),
            Times.Once);
    }

    [Fact]
    public async Task GivenInvalidJson_WhenGetSettingsAsync_ThenReturnsDefaultSettings()
    {
        // Arrange
        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<string?>("localStorage.getItem", It.Is<object?[]>(args => (string?)args[0] == SettingsKey)))
            .ReturnsAsync("invalid json {{{");

        var sut = this.CreateSut();

        // Act
        var result = await sut.GetSettingsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("light", result.Theme);
    }

    [Fact]
    public async Task GivenCachedSettings_WhenGetSettingsAsync_ThenDoesNotCallJavaScriptAgain()
    {
        // Arrange
        var settings = new UserSettings { Theme = "dark" };
        var json = JsonSerializer.Serialize(settings);
        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<string?>("localStorage.getItem", It.IsAny<object?[]>()))
            .ReturnsAsync(json);

        var sut = this.CreateSut();

        // Act
        await sut.GetSettingsAsync();
        await sut.GetSettingsAsync();

        // Assert
        this.mockJsRuntime.Verify(
            x => x.InvokeAsync<string?>("localStorage.getItem", It.IsAny<object?[]>()),
            Times.Once);
    }

    [Fact]
    public async Task GivenSettings_WhenSaveSettingsAsync_ThenSerializesAndStores()
    {
        // Arrange
        string? capturedJson = null;
        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<IJSVoidResult>("localStorage.setItem", It.IsAny<object?[]>()))
            .Callback<string, object?[]>((_, args) => capturedJson = args[1] as string)
            .ReturnsAsync(Mock.Of<IJSVoidResult>());

        var sut = this.CreateSut();
        var settings = new UserSettings { Theme = "dark" };

        // Act
        await sut.SaveSettingsAsync(settings);

        // Assert
        Assert.NotNull(capturedJson);
        Assert.Contains("dark", capturedJson);
        this.mockJsRuntime.Verify(
            x => x.InvokeAsync<IJSVoidResult>("localStorage.setItem", It.Is<object?[]>(args => (string?)args[0] == SettingsKey)),
            Times.Once);
    }

    [Fact]
    public async Task GivenSettings_WhenSaveSettingsAsync_ThenUpdatesCachedSettings()
    {
        // Arrange
        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<IJSVoidResult>("localStorage.setItem", It.IsAny<object?[]>()))
            .ReturnsAsync(Mock.Of<IJSVoidResult>());

        var sut = this.CreateSut();
        var settings = new UserSettings { Theme = "dark" };

        // Act
        await sut.SaveSettingsAsync(settings);
        var result = await sut.GetSettingsAsync();

        // Assert
        Assert.Equal("dark", result.Theme);
        // localStorage.getItem should NOT have been called since settings are cached
        this.mockJsRuntime.Verify(
            x => x.InvokeAsync<string?>("localStorage.getItem", It.IsAny<object?[]>()),
            Times.Never);
    }

    [Fact]
    public async Task GivenJsException_WhenSaveSettingsAsync_ThenHandlesSilently()
    {
        // Arrange
        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<IJSVoidResult>("localStorage.setItem", It.IsAny<object?[]>()))
            .ThrowsAsync(new JSException("Storage full"));

        var sut = this.CreateSut();

        // Act & Assert
        var exception = await Record.ExceptionAsync(() =>
            sut.SaveSettingsAsync(new UserSettings { Theme = "dark" }));

        Assert.Null(exception);
    }

    [Fact]
    public async Task GivenStoredSettings_WhenGetThemeAsync_ThenReturnsTheme()
    {
        // Arrange
        var settings = new UserSettings { Theme = "dark" };
        var json = JsonSerializer.Serialize(settings);
        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<string?>("localStorage.getItem", It.IsAny<object?[]>()))
            .ReturnsAsync(json);

        var sut = this.CreateSut();

        // Act
        var theme = await sut.GetThemeAsync();

        // Assert
        Assert.Equal("dark", theme);
    }

    [Fact]
    public async Task GivenNewTheme_WhenSetThemeAsync_ThenSavesUpdatedTheme()
    {
        // Arrange
        var settings = new UserSettings { Theme = "light" };
        var json = JsonSerializer.Serialize(settings);
        string? savedJson = null;

        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<string?>("localStorage.getItem", It.IsAny<object?[]>()))
            .ReturnsAsync(json);
        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<IJSVoidResult>("localStorage.setItem", It.IsAny<object?[]>()))
            .Callback<string, object?[]>((_, args) => savedJson = args[1] as string)
            .ReturnsAsync(Mock.Of<IJSVoidResult>());

        var sut = this.CreateSut();

        // Act
        await sut.SetThemeAsync("dark");

        // Assert
        Assert.NotNull(savedJson);
        Assert.Contains("dark", savedJson);
    }
}
