using Clypse.Portal.Application.Services;
using Clypse.Portal.Models.Settings;
using Devoctomy.Blazwiz.Browser.Services.Interfaces;
using Moq;

namespace Clypse.Portal.Application.UnitTests.Services;

public class VaultReportPreferencesServiceTests
{
    private readonly Mock<ILocalStorageService> mockStorage = new();
    private readonly ClypseSettings settings = new() { OldPasswordMonths = 6 };

    [Fact]
    public void GivenNullDependencies_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Equal("localStorage", Assert.Throws<ArgumentNullException>(() => new VaultReportPreferencesService(null!, settings)).ParamName);
        Assert.Equal("settings", Assert.Throws<ArgumentNullException>(() => new VaultReportPreferencesService(mockStorage.Object, null!)).ParamName);
    }

    [Fact]
    public async Task GivenNothingSaved_WhenRead_ThenTheAgeIsTheAppsDefault()
    {
        // Act
        var preferences = await CreateSut().GetAsync();

        // Assert
        Assert.Equal(6, preferences.OldPasswordMonths);
    }

    [Fact]
    public async Task GivenPreferencesSaved_WhenRead_ThenTheyAreKept_AndStorageIsReadOnce()
    {
        // Arrange
        mockStorage.Setup(x => x.GetObjectAsync<VaultReportPreferences>(VaultReportPreferencesService.StorageName))
            .ReturnsAsync(new VaultReportPreferences { OldPasswordMonths = 12 });
        var sut = CreateSut();

        // Act
        var first = await sut.GetAsync();
        var second = await sut.GetAsync();

        // Assert
        Assert.Equal(12, first.OldPasswordMonths);
        Assert.Equal(12, second.OldPasswordMonths);
        mockStorage.Verify(x => x.GetObjectAsync<VaultReportPreferences>(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task GivenAnAgeUnderTheMinimumKept_WhenRead_ThenTheMinimum()
    {
        // Arrange
        mockStorage.Setup(x => x.GetObjectAsync<VaultReportPreferences>(VaultReportPreferencesService.StorageName))
            .ReturnsAsync(new VaultReportPreferences { OldPasswordMonths = 0 });

        // Act
        var preferences = await CreateSut().GetAsync();

        // Assert
        Assert.Equal(VaultReportPreferences.MinimumMonths, preferences.OldPasswordMonths);
    }

    [Fact]
    public async Task GivenPreferences_WhenSaved_ThenTheyAreStored_ReadBackWithoutStorage_AndChangedIsRaised()
    {
        // Arrange
        var sut = CreateSut();
        var changes = 0;
        sut.Changed += (_, _) => changes++;

        // Act
        await sut.SaveAsync(new VaultReportPreferences { OldPasswordMonths = -2 });
        var read = await sut.GetAsync();

        // Assert
        mockStorage.Verify(x => x.SetObjectAsync(VaultReportPreferencesService.StorageName, It.Is<VaultReportPreferences>(p => p.OldPasswordMonths == VaultReportPreferences.MinimumMonths)), Times.Once);
        Assert.Equal(VaultReportPreferences.MinimumMonths, read.OldPasswordMonths);
        Assert.Equal(1, changes);
        mockStorage.Verify(x => x.GetObjectAsync<VaultReportPreferences>(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GivenUnreadablePreferences_WhenRead_ThenTheDefaults()
    {
        // Arrange
        mockStorage.Setup(x => x.GetObjectAsync<VaultReportPreferences>(It.IsAny<string>())).ThrowsAsync(new System.Text.Json.JsonException());

        // Act
        var preferences = await CreateSut().GetAsync();

        // Assert
        Assert.Equal(6, preferences.OldPasswordMonths);
    }

    [Fact]
    public async Task GivenNull_WhenSaved_ThenThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => CreateSut().SaveAsync(null!));
    }

    private VaultReportPreferencesService CreateSut() => new(mockStorage.Object, settings);
}
