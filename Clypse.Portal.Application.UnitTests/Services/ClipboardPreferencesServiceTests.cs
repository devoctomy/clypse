using Clypse.Portal.Application.Services;
using Clypse.Portal.Models.Settings;
using Devoctomy.Blazwiz.Browser.Services.Interfaces;
using Moq;

namespace Clypse.Portal.Application.UnitTests.Services;

public class ClipboardPreferencesServiceTests
{
    private readonly Mock<ILocalStorageService> mockStorage = new();
    private readonly ClypseSettings settings = new() { ClipboardClearSeconds = 45 };

    [Fact]
    public void GivenNullDependencies_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Equal("localStorage", Assert.Throws<ArgumentNullException>(() => new ClipboardPreferencesService(null!, settings)).ParamName);
        Assert.Equal("settings", Assert.Throws<ArgumentNullException>(() => new ClipboardPreferencesService(mockStorage.Object, null!)).ParamName);
    }

    [Fact]
    public async Task GivenNothingSaved_WhenRead_ThenSecretsAreClearedAfterTheAppsDefault()
    {
        // Act
        var preferences = await CreateSut().GetAsync();

        // Assert
        Assert.True(preferences.ClearSecrets);
        Assert.Equal(45, preferences.ClearAfterSeconds);
    }

    [Fact]
    public async Task GivenPreferencesSaved_WhenRead_ThenTheyAreKept_AndStorageIsReadOnce()
    {
        // Arrange
        mockStorage.Setup(x => x.GetObjectAsync<ClipboardPreferences>(ClipboardPreferencesService.StorageName))
            .ReturnsAsync(new ClipboardPreferences { ClearSecrets = false, ClearAfterSeconds = 10 });
        var sut = CreateSut();

        // Act
        var first = await sut.GetAsync();
        var second = await sut.GetAsync();

        // Assert
        Assert.False(first.ClearSecrets);
        Assert.Equal(10, second.ClearAfterSeconds);
        mockStorage.Verify(x => x.GetObjectAsync<ClipboardPreferences>(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task GivenPreferences_WhenSaved_ThenTheyAreStored_AndReadBackWithoutStorage()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.SaveAsync(new ClipboardPreferences { ClearSecrets = false, ClearAfterSeconds = 1 });
        var read = await sut.GetAsync();

        // Assert
        mockStorage.Verify(x => x.SetObjectAsync(ClipboardPreferencesService.StorageName, It.Is<ClipboardPreferences>(p => !p.ClearSecrets && p.ClearAfterSeconds == ClipboardPreferences.MinimumSeconds)), Times.Once);
        Assert.False(read.ClearSecrets);
        Assert.Equal(ClipboardPreferences.MinimumSeconds, read.ClearAfterSeconds);
        mockStorage.Verify(x => x.GetObjectAsync<ClipboardPreferences>(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GivenUnreadablePreferences_WhenRead_ThenTheDefaults()
    {
        // Arrange
        mockStorage.Setup(x => x.GetObjectAsync<ClipboardPreferences>(It.IsAny<string>())).ThrowsAsync(new System.Text.Json.JsonException());

        // Act
        var preferences = await CreateSut().GetAsync();

        // Assert
        Assert.True(preferences.ClearSecrets);
        Assert.Equal(45, preferences.ClearAfterSeconds);
    }

    [Fact]
    public async Task GivenNull_WhenSaved_ThenThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => CreateSut().SaveAsync(null!));
    }

    private ClipboardPreferencesService CreateSut() => new(mockStorage.Object, settings);
}
