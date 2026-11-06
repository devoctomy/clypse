using Clypse.Core.Enums;
using Clypse.Core.Password;
using Clypse.Portal.Application.Services.Interfaces;
using Clypse.Portal.Application.Text;
using Clypse.Portal.Application.ViewModels;
using Clypse.Portal.Models.Settings;
using Clypse.Portal.Models.Vaults;
using Devoctomy.Blazwiz.Testing;
using Moq;

namespace Clypse.Portal.Application.UnitTests.ViewModels;

public class PasswordGeneratorViewModelTests
{
    private const CharacterGroup All = CharacterGroup.Lowercase | CharacterGroup.Uppercase | CharacterGroup.Digits | CharacterGroup.Special;

    private readonly FrameworkText text = new(typeof(ClypseText));
    private readonly Mock<IPasswordGeneratorService> mockGenerator = new();
    private readonly Mock<IPasswordStrengthService> mockStrength = new();
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
    }

    [Fact]
    public void GivenNullDependencies_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Arrange
        var t = text.For<ClypseText>();

        // Act & Assert
        Assert.Equal("generator", Assert.Throws<ArgumentNullException>(() => new PasswordGeneratorViewModel(null!, mockStrength.Object, settings, t)).ParamName);
        Assert.Equal("strengthService", Assert.Throws<ArgumentNullException>(() => new PasswordGeneratorViewModel(mockGenerator.Object, null!, settings, t)).ParamName);
        Assert.Equal("settings", Assert.Throws<ArgumentNullException>(() => new PasswordGeneratorViewModel(mockGenerator.Object, mockStrength.Object, null!, t)).ParamName);
        Assert.Equal("text", Assert.Throws<ArgumentNullException>(() => new PasswordGeneratorViewModel(mockGenerator.Object, mockStrength.Object, settings, null!)).ParamName);
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
    public void GivenTemplates_WhenConstructed_ThenOnlyThoseWithATemplateAreOffered_TheFirstChosen()
    {
        // Act
        var sut = CreateSut();

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
        await sut.SetMemorableAsync(true);

        // Act
        await sut.SetTemplateAsync("Gone");

        // Assert
        Assert.Equal("Words", sut.Template);
    }

    [Fact]
    public async Task GivenNoTemplates_WhenMemorableIsChosen_ThenRandomPasswordsAreStillMade()
    {
        // Arrange
        settings.MemorablePasswordTemplates.Clear();
        var sut = CreateSut();

        // Act
        await sut.SetMemorableAsync(true);

        // Assert
        Assert.False(sut.CanMakeMemorable);
        Assert.False(sut.IsMemorable);
        Assert.Equal(new string('x', 20), sut.Password);
    }

    private PasswordGeneratorViewModel CreateSut() => new(mockGenerator.Object, mockStrength.Object, settings, text.For<ClypseText>());
}
