using Clypse.Core.Enums;
using Clypse.Core.Password;
using Clypse.Portal.Application.Services;
using Clypse.Portal.Application.Text;
using Clypse.Portal.Application.ViewModels;
using Clypse.Portal.Models.Settings;
using Devoctomy.Blazwiz.Browser.Services.Interfaces;
using Devoctomy.Blazwiz.Testing;
using Devoctomy.Blazwiz.UI.Models;
using Devoctomy.Blazwiz.UI.Services.Interfaces;
using Moq;

namespace Clypse.Portal.Application.UnitTests.ViewModels;

public class PasswordGeneratorSettingsViewModelTests
{
    private readonly FrameworkText text = new(typeof(ClypseText));
    private readonly Mock<ILocalStorageService> mockStorage = new();
    private readonly Mock<IPasswordGeneratorService> mockGenerator = new();
    private readonly Mock<IToastService> mockToasts = new();
    private readonly ClypseSettings settings = new()
    {
        MemorablePasswordTemplates = [new MemorablePasswordTemplate { Name = "Default", Template = "{dict(noun):random}" }],
    };

    private PasswordGeneratorPreferences? stored;

    public PasswordGeneratorSettingsViewModelTests()
    {
        mockStorage.Setup(x => x.GetObjectAsync<PasswordGeneratorPreferences>(PasswordGeneratorPreferencesService.StorageName)).ReturnsAsync(() => stored);
        mockStorage.Setup(x => x.SetObjectAsync(PasswordGeneratorPreferencesService.StorageName, It.IsAny<PasswordGeneratorPreferences>()))
            .Callback<string, PasswordGeneratorPreferences>((_, p) => stored = p)
            .Returns(Task.CompletedTask);
        mockGenerator.Setup(x => x.GenerateMemorablePasswordAsync(It.IsAny<string>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string template, bool _, CancellationToken _) => template.Contains("bad", StringComparison.Ordinal) ? throw new FormatException("bad") : "Word42");
    }

    [Fact]
    public void GivenNullDependencies_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Arrange
        var preferences = Preferences();
        var t = text.For<ClypseText>();

        // Act & Assert
        Assert.Equal("preferencesService", Assert.Throws<ArgumentNullException>(() => new PasswordGeneratorSettingsViewModel(null!, mockGenerator.Object, settings, mockToasts.Object, t)).ParamName);
        Assert.Equal("generator", Assert.Throws<ArgumentNullException>(() => new PasswordGeneratorSettingsViewModel(preferences, null!, settings, mockToasts.Object, t)).ParamName);
        Assert.Equal("settings", Assert.Throws<ArgumentNullException>(() => new PasswordGeneratorSettingsViewModel(preferences, mockGenerator.Object, null!, mockToasts.Object, t)).ParamName);
        Assert.Equal("toasts", Assert.Throws<ArgumentNullException>(() => new PasswordGeneratorSettingsViewModel(preferences, mockGenerator.Object, settings, null!, t)).ParamName);
        Assert.Equal("text", Assert.Throws<ArgumentNullException>(() => new PasswordGeneratorSettingsViewModel(preferences, mockGenerator.Object, settings, mockToasts.Object, null!)).ParamName);
    }

    [Fact]
    public async Task GivenNothingSaved_WhenInitialized_ThenTheDefaultsAreShown()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.OnInitializedAsync();

        // Assert
        Assert.True(sut.IsLoaded);
        Assert.False(sut.IsMemorable);
        Assert.Equal(20, sut.Length);
        Assert.Equal("Length: 20", sut.LengthText);
        Assert.True(sut.AtLeastOneOfEach);
        Assert.All(PasswordGeneratorViewModel.Groups, g => Assert.True(sut.Uses(g)));
        Assert.Equal("Default", Assert.Single(sut.BuiltInTemplates).Name);
        Assert.Empty(sut.UserTemplates);
        Assert.Equal("Default", sut.Template);
    }

    [Fact]
    public async Task GivenChanges_WhenSaved_ThenTheyAreKept_AndTheToastShown()
    {
        // Arrange
        var sut = CreateSut();
        await sut.OnInitializedAsync();

        // Act
        sut.IsMemorable = true;
        sut.Length = 100;
        sut.AtLeastOneOfEach = false;
        sut.SetGroup(CharacterGroup.Special, false);
        await sut.SaveCommand.ExecuteAsync(null);

        // Assert
        Assert.True(stored!.IsMemorable);
        Assert.Equal(PasswordGeneratorViewModel.MaximumLength, stored.Length);
        Assert.False(stored.AtLeastOneOfEach);
        Assert.Equal(CharacterGroup.Lowercase | CharacterGroup.Uppercase | CharacterGroup.Digits, stored.Groups);
        Assert.Equal("Default", stored.Template);
        mockToasts.Verify(x => x.Show("Password generator settings saved.", ToastTone.Success, null), Times.Once);
    }

    [Fact]
    public async Task GivenOnlyOneKindOfCharacter_WhenItIsTurnedOff_ThenItStaysOn()
    {
        // Arrange
        var sut = CreateSut();
        await sut.OnInitializedAsync();
        sut.SetGroup(CharacterGroup.Lowercase, false);
        sut.SetGroup(CharacterGroup.Uppercase, false);
        sut.SetGroup(CharacterGroup.Special, false);

        // Act
        sut.SetGroup(CharacterGroup.Digits, false);

        // Assert
        Assert.True(sut.Uses(CharacterGroup.Digits));
    }

    [Fact]
    public async Task GivenANewTemplate_WhenTriedAndAdded_ThenAnExampleIsShown_AndItIsOfferedAndChosen()
    {
        // Arrange
        var sut = CreateSut();
        await sut.OnInitializedAsync();
        sut.NewTemplateName = " Mine ";
        sut.NewTemplate = "{dict(verb):random}{randstr(0123456789,2)}";

        // Act
        await sut.TryTemplateCommand.ExecuteAsync(null);
        var example = sut.Example;
        await sut.AddTemplateCommand.ExecuteAsync(null);
        sut.Template = "Mine";
        await sut.SaveCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal("Word42", example);
        Assert.Null(sut.Error);
        Assert.Equal("Mine", Assert.Single(sut.UserTemplates).Name);
        Assert.Equal(["Default", "Mine"], sut.Templates.Select(t => t.Name));
        Assert.Equal(string.Empty, sut.NewTemplateName);
        Assert.Equal("Mine", stored!.Template);
        Assert.Equal("{dict(verb):random}{randstr(0123456789,2)}", Assert.Single(stored.UserTemplates).Template);
    }

    [Theory]
    [InlineData("", "{dict(verb):random}", "Give the template a name.")]
    [InlineData("default", "{dict(verb):random}", "There is already a template called default.")]
    [InlineData("Mine", "{bad}", "That template could not make a password.")]
    [InlineData("Mine", " ", "That template could not make a password.")]
    public async Task GivenAnInvalidTemplate_WhenAdded_ThenItSaysWhy_AndNothingIsAdded(string name, string template, string expected)
    {
        // Arrange
        var sut = CreateSut();
        await sut.OnInitializedAsync();
        sut.NewTemplateName = name;
        sut.NewTemplate = template;

        // Act
        await sut.AddTemplateCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal(expected, sut.Error);
        Assert.Empty(sut.UserTemplates);
    }

    [Fact]
    public async Task GivenATemplateThatFails_WhenTried_ThenItSaysSo()
    {
        // Arrange
        var sut = CreateSut();
        await sut.OnInitializedAsync();
        sut.NewTemplate = "{bad}";

        // Act
        await sut.TryTemplateCommand.ExecuteAsync(null);

        // Assert
        Assert.Null(sut.Example);
        Assert.Equal("That template could not make a password.", sut.Error);
    }

    [Fact]
    public async Task GivenAnOwnTemplate_WhenRemoved_ThenItIsNoLongerOffered()
    {
        // Arrange
        stored = new PasswordGeneratorPreferences { UserTemplates = [new MemorablePasswordTemplate { Name = "Mine", Template = "{dict(verb):random}" }] };
        var sut = CreateSut();
        await sut.OnInitializedAsync();

        // Act
        sut.RemoveTemplateCommand.Execute("Mine");

        // Assert
        Assert.Empty(sut.UserTemplates);
        Assert.Equal(["Default"], sut.Templates.Select(t => t.Name));
    }

    [Fact]
    public void GivenTheNewTemplateFields_WhenSetToNull_ThenTheyAreEmpty()
    {
        // Arrange
        var sut = CreateSut();
        sut.NewTemplateName = "Mine";
        sut.NewTemplate = "{dict(noun):random}";

        // Act
        sut.NewTemplateName = null!;
        sut.NewTemplate = null!;

        // Assert
        Assert.Empty(sut.NewTemplateName);
        Assert.Empty(sut.NewTemplate);
    }

    [Fact]
    public void GivenBuiltInTemplatesWithoutANameOrATemplate_WhenListed_ThenTheyAreLeftOut()
    {
        // Arrange
        settings.MemorablePasswordTemplates.Add(new MemorablePasswordTemplate { Name = " ", Template = "{dict(verb):random}" });
        settings.MemorablePasswordTemplates.Add(new MemorablePasswordTemplate { Name = "Empty", Template = " " });
        var sut = CreateSut();

        // Act
        var templates = sut.BuiltInTemplates;

        // Assert
        Assert.Equal(["Default"], templates.Select(t => t.Name));
    }

    [Fact]
    public async Task GivenNoTemplates_WhenInitialized_ThenNoneIsChosen()
    {
        // Arrange
        settings.MemorablePasswordTemplates.Clear();
        var sut = CreateSut();

        // Act
        await sut.OnInitializedAsync();

        // Assert
        Assert.Null(sut.Template);
    }

    [Fact]
    public async Task GivenAKindOfCharacterTurnedOff_WhenTurnedOnAgain_ThenItIsUsed()
    {
        // Arrange
        var sut = CreateSut();
        await sut.OnInitializedAsync();
        sut.SetGroup(CharacterGroup.Special, false);
        Assert.False(sut.Uses(CharacterGroup.Special));

        // Act
        sut.SetGroup(CharacterGroup.Special, true);

        // Assert
        Assert.True(sut.Uses(CharacterGroup.Special));
    }

    [Fact]
    public async Task GivenATemplateThatMakesNoPassword_WhenTried_ThenItSaysSo()
    {
        // Arrange
        mockGenerator.Setup(x => x.GenerateMemorablePasswordAsync("{dict(none)}", false, It.IsAny<CancellationToken>())).ReturnsAsync(string.Empty);
        var sut = CreateSut();
        await sut.OnInitializedAsync();
        sut.NewTemplate = "{dict(none)}";

        // Act
        await sut.TryTemplateCommand.ExecuteAsync(null);

        // Assert
        Assert.Null(sut.Example);
        Assert.Equal("That template could not make a password.", sut.Error);
    }

    private PasswordGeneratorPreferencesService Preferences() => new(mockStorage.Object, settings);

    private PasswordGeneratorSettingsViewModel CreateSut() => new(Preferences(), mockGenerator.Object, settings, mockToasts.Object, text.For<ClypseText>());
}
