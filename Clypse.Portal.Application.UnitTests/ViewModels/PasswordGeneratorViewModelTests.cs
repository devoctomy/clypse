using Clypse.Core.Enums;
using Clypse.Core.Password;
using Clypse.Portal.Application.Services;
using Clypse.Portal.Application.Services.Interfaces;
using Clypse.Portal.Application.Text;
using Clypse.Portal.Application.ViewModels;
using Clypse.Portal.Models.Settings;
using Clypse.Portal.Models.Vaults;
using Devoctomy.Blazwiz.Browser.Services.Interfaces;
using Devoctomy.Blazwiz.Testing;
using Moq;

namespace Clypse.Portal.Application.UnitTests.ViewModels;

public class PasswordGeneratorViewModelTests
{
    private const CharacterGroup All = CharacterGroup.Lowercase | CharacterGroup.Uppercase | CharacterGroup.Digits | CharacterGroup.Special;

    private readonly FrameworkText text = new(typeof(ClypseText));
    private readonly Mock<IPasswordGeneratorService> mockGenerator = new();
    private readonly Mock<IPasswordStrengthService> mockStrength = new();
    private readonly Mock<ILocalStorageService> mockStorage = new();
    private PasswordGeneratorPreferences? stored;

    private readonly ClypseSettings settings = new()
    {
        MemorablePasswordTemplates =
        [
            new MemorablePasswordTemplate { Name = "Words", Template = "{dict(noun):random}" },
            new MemorablePasswordTemplate { Name = "Blank", Template = " " },
            new MemorablePasswordTemplate { Name = "Long", Template = "{dict(noun):random}{dict(verb):random}" },
        ],
    };

    public PasswordGeneratorViewModelTests()
    {
        mockGenerator.Setup(x => x.GenerateRandomPassword(It.IsAny<CharacterGroup>(), It.IsAny<int>(), It.IsAny<bool>()))
            .Returns((CharacterGroup g, int length, bool _) => new string('x', length));
        mockGenerator.Setup(x => x.GenerateMemorablePasswordAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string template, bool shuffle, CancellationToken _) => $"{template}|{shuffle}");
        mockStrength.Setup(x => x.EstimateAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PasswordStrength { Estimation = PasswordComplexityEstimation.Strong, Entropy = 99 });
        mockStrength.Setup(x => x.Describe(It.IsAny<PasswordStrength>())).Returns("Strong");
        mockStorage.Setup(x => x.GetObjectAsync<PasswordGeneratorPreferences>(PasswordGeneratorPreferencesService.StorageName)).ReturnsAsync(() => stored);
    }

    [Fact]
    public void GivenNullDependencies_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Arrange
        var t = text.For<ClypseText>();

        // Act & Assert
        Assert.Equal("generator", Assert.Throws<ArgumentNullException>(() => new PasswordGeneratorViewModel(null!, mockStrength.Object, Preferences(), t)).ParamName);
        Assert.Equal("strengthService", Assert.Throws<ArgumentNullException>(() => new PasswordGeneratorViewModel(mockGenerator.Object, null!, Preferences(), t)).ParamName);
        Assert.Equal("preferencesService", Assert.Throws<ArgumentNullException>(() => new PasswordGeneratorViewModel(mockGenerator.Object, mockStrength.Object, null!, t)).ParamName);
        Assert.Equal("text", Assert.Throws<ArgumentNullException>(() => new PasswordGeneratorViewModel(mockGenerator.Object, mockStrength.Object, Preferences(), null!)).ParamName);
    }

    [Fact]
    public async Task GivenTheDefaults_WhenInitialized_ThenA20CharacterRandomPasswordOfEveryKind()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.OnInitializedAsync();

        // Assert
        Assert.False(sut.IsMemorable);
        Assert.Equal(new string('x', 20), sut.Password);
        Assert.Equal("Length: 20", sut.LengthText);
        Assert.Equal("Strong", sut.StrengthText);
        Assert.All(PasswordGeneratorViewModel.Groups, g => Assert.True(sut.Uses(g)));
        mockGenerator.Verify(x => x.GenerateRandomPassword(All, 20, true), Times.Once);
    }

    [Fact]
    public async Task GivenTemplates_WhenInitialized_ThenOnlyThoseWithATemplateAreOffered_TheFirstChosen()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.OnInitializedAsync();

        // Assert
        Assert.True(sut.CanMakeMemorable);
        Assert.Equal(["Words", "Long"], sut.Templates.Select(t => t.Name));
        Assert.Equal("Words", sut.Template);
    }

    [Theory]
    [InlineData(3, 8)]
    [InlineData(30, 30)]
    [InlineData(500, 64)]
    public async Task GivenALength_WhenSet_ThenItIsKeptInRangeAndUsed(int length, int expected)
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.SetLengthAsync(length);

        // Assert
        Assert.Equal(expected, sut.Length);
        Assert.Equal(expected, sut.Password.Length);
    }

    [Fact]
    public async Task GivenAKindOfCharacter_WhenTurnedOff_ThenItIsNotUsed()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.SetGroupAsync(CharacterGroup.Special, false);
        await sut.SetAtLeastOneOfEachAsync(false);

        // Assert
        Assert.False(sut.Uses(CharacterGroup.Special));
        Assert.False(sut.AtLeastOneOfEach);
        mockGenerator.Verify(x => x.GenerateRandomPassword(All & ~CharacterGroup.Special, 20, false), Times.Once);
    }

    [Fact]
    public async Task GivenNoKindOfCharacter_WhenGenerating_ThenItSaysToChooseOne()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        foreach (var group in PasswordGeneratorViewModel.Groups)
        {
            await sut.SetGroupAsync(group, false);
        }

        // Assert
        Assert.Equal(string.Empty, sut.Password);
        Assert.Equal("Choose at least one kind of character.", sut.Error);
        await sut.SetGroupAsync(CharacterGroup.Digits, true);
        Assert.Null(sut.Error);
    }

    [Fact]
    public async Task GivenMemorable_WhenChosen_ThenThePasswordIsMadeFromTheChosenTemplate()
    {
        // Arrange
        var sut = CreateSut();
        await sut.OnInitializedAsync();

        // Act
        await sut.SetMemorableAsync(true);
        await sut.SetTemplateAsync("Long");
        await sut.SetShuffleAsync(true);

        // Assert
        Assert.True(sut.IsMemorable);
        Assert.Equal("Long", sut.Template);
        Assert.Equal("{dict(noun):random}{dict(verb):random}|True", sut.Password);
    }

    [Fact]
    public async Task GivenAnUnknownTemplate_WhenChosen_ThenTheFirstIsUsed()
    {
        // Arrange
        var sut = CreateSut();
        await sut.OnInitializedAsync();
        await sut.SetMemorableAsync(true);

        // Act
        await sut.SetTemplateAsync("Gone");

        // Assert
        Assert.Equal("Words", sut.Template);
    }

    [Fact]
    public async Task GivenNoTemplates_WhenOneIsChosen_ThenNoneIsUsed()
    {
        // Arrange
        settings.MemorablePasswordTemplates.Clear();
        var sut = CreateSut();
        await sut.OnInitializedAsync();

        // Act
        await sut.SetTemplateAsync("Words");

        // Assert
        Assert.Null(sut.Template);
    }

    [Fact]
    public async Task GivenNoTemplates_WhenMemorableIsChosen_ThenRandomPasswordsAreStillMade()
    {
        // Arrange
        settings.MemorablePasswordTemplates.Clear();
        var sut = CreateSut();
        await sut.OnInitializedAsync();

        // Act
        await sut.SetMemorableAsync(true);

        // Assert
        Assert.False(sut.CanMakeMemorable);
        Assert.False(sut.IsMemorable);
        Assert.Equal(new string('x', 20), sut.Password);
    }

    [Fact]
    public async Task GivenSavedPreferences_WhenInitialized_ThenTheGeneratorStartsWithThem_AndTheUsersTemplatesAreOffered()
    {
        // Arrange
        stored = new PasswordGeneratorPreferences
        {
            IsMemorable = true,
            Length = 32,
            Groups = CharacterGroup.Digits,
            AtLeastOneOfEach = false,
            Template = "Mine",
            UserTemplates = [new MemorablePasswordTemplate { Name = "Mine", Template = "{dict(verb):random}" }],
        };
        var sut = CreateSut();

        // Act
        await sut.OnInitializedAsync();

        // Assert
        Assert.True(sut.IsMemorable);
        Assert.Equal(["Words", "Long", "Mine"], sut.Templates.Select(t => t.Name));
        Assert.Equal("Mine", sut.Template);
        Assert.Equal("{dict(verb):random}|False", sut.Password);
        await sut.SetMemorableAsync(false);
        Assert.Equal(32, sut.Length);
        Assert.False(sut.AtLeastOneOfEach);
        mockGenerator.Verify(x => x.GenerateRandomPassword(CharacterGroup.Digits, 32, false), Times.Once);
    }

    private PasswordGeneratorPreferencesService Preferences() => new(mockStorage.Object, settings);

    private PasswordGeneratorViewModel CreateSut() => new(mockGenerator.Object, mockStrength.Object, Preferences(), text.For<ClypseText>());
}
