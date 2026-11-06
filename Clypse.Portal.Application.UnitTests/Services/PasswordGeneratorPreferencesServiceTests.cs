using Clypse.Core.Enums;
using Clypse.Portal.Application.Services;
using Clypse.Portal.Models.Settings;
using Devoctomy.Blazwiz.Browser.Services.Interfaces;
using Moq;

namespace Clypse.Portal.Application.UnitTests.Services;

public class PasswordGeneratorPreferencesServiceTests
{
    private readonly Mock<ILocalStorageService> mockStorage = new();
    private readonly ClypseSettings settings = new()
    {
        MemorablePasswordTemplates =
        [
            new MemorablePasswordTemplate { Name = "Default", Template = "{dict(noun):random}" },
            new MemorablePasswordTemplate { Name = "Empty", Template = " " },
        ],
    };

    [Fact]
    public void GivenNullDependencies_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Equal("localStorage", Assert.Throws<ArgumentNullException>(() => new PasswordGeneratorPreferencesService(null!, settings)).ParamName);
        Assert.Equal("settings", Assert.Throws<ArgumentNullException>(() => new PasswordGeneratorPreferencesService(mockStorage.Object, null!)).ParamName);
    }

    [Fact]
    public async Task GivenNothingSaved_WhenRead_ThenTheDefaults()
    {
        // Act
        var preferences = await CreateSut().GetAsync();

        // Assert
        Assert.False(preferences.IsMemorable);
        Assert.Equal(20, preferences.Length);
        Assert.Equal(CharacterGroup.Lowercase | CharacterGroup.Uppercase | CharacterGroup.Digits | CharacterGroup.Special, preferences.Groups);
        Assert.True(preferences.AtLeastOneOfEach);
        Assert.Empty(preferences.UserTemplates);
    }

    [Fact]
    public async Task GivenUnreadablePreferences_WhenRead_ThenTheDefaults()
    {
        // Arrange
        mockStorage.Setup(x => x.GetObjectAsync<PasswordGeneratorPreferences>(PasswordGeneratorPreferencesService.StorageName))
            .ThrowsAsync(new System.Text.Json.JsonException("bad"));

        // Act
        var preferences = await CreateSut().GetAsync();

        // Assert
        Assert.Equal(20, preferences.Length);
    }

    [Fact]
    public async Task GivenPreferences_WhenSaved_ThenTheyAreKeptUnderTheirName()
    {
        // Arrange
        var preferences = new PasswordGeneratorPreferences { Length = 30 };

        // Act
        await CreateSut().SaveAsync(preferences);

        // Assert
        mockStorage.Verify(x => x.SetObjectAsync(PasswordGeneratorPreferencesService.StorageName, preferences), Times.Once);
        await Assert.ThrowsAsync<ArgumentNullException>(() => CreateSut().SaveAsync(null!));
    }

    [Fact]
    public void GivenBuiltInAndOwnTemplates_WhenListed_ThenBuiltInFirst_ThenOwn_EachNameOnceAndOnlyWithATemplate()
    {
        // Arrange
        var preferences = new PasswordGeneratorPreferences
        {
            UserTemplates =
            [
                new MemorablePasswordTemplate { Name = "Mine", Template = "{dict(verb):random}" },
                new MemorablePasswordTemplate { Name = "default", Template = "{dict(adjective):random}" },
                new MemorablePasswordTemplate { Name = " ", Template = "{dict(verb):random}" },
            ],
        };

        // Act
        var templates = CreateSut().GetTemplates(preferences);

        // Assert
        Assert.Equal(["Default", "Mine"], templates.Select(t => t.Name));
    }

    private PasswordGeneratorPreferencesService CreateSut() => new(mockStorage.Object, settings);
}
