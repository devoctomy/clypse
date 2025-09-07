using clypse.core.Cryptogtaphy;
using clypse.core.Password;

namespace clypse.core.UnitTests.Password;

public class PasswordGeneratorServiceTests : IDisposable
{
    private readonly RandomGeneratorService randomGeneratorService;
    private readonly List<IPasswordGeneratorTokenProcessor> tokenProcessors;
    private readonly PasswordGeneratorService sut;

    public PasswordGeneratorServiceTests()
    {
        this.randomGeneratorService = new RandomGeneratorService();
        this.tokenProcessors =
        [
            new DictionaryTokenProcessor(),
            new RandomStringTokenProcessor(),
        ];
        this.sut = new PasswordGeneratorService(
            this.randomGeneratorService,
            this.tokenProcessors);
    }

    [Theory]
    [InlineData("{randstr(abcdefg)}")]
    public void GivenInvalidTemplate_WhenLoadDictionary_ThenEmptyStringReturned(string invalidTemplate)
    {
        // Arrange & Act
        var result = this.sut.GenerateMemorablePassword(invalidTemplate);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GivenDictionaryType_WhenLoadDictionary_ThenReturnsListOfWords()
    {
        // Arrange
        var dictionaryType = Enums.DictionaryType.Adjective;

        // Act
        var result = this.sut.GetOrLoadDictionary(dictionaryType);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public void GivenTemplateWithDictionarySelections_WhenGenerateMemorablePassword_ThenReturnsPassword()
    {
        // Arrange
        var template = "{dict(adjective):upper}-{dict(noun):lower}-{dict(verb):upper}";
        var adjectives = this.sut.GetOrLoadDictionary(Enums.DictionaryType.Adjective);
        var nouns = this.sut.GetOrLoadDictionary(Enums.DictionaryType.Noun);
        var verbs = this.sut.GetOrLoadDictionary(Enums.DictionaryType.Verb);

        // Act
        var result = this.sut.GenerateMemorablePassword(template);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        var parts = result.Split('-');
        Assert.Contains(adjectives, x => x.Equals(parts[0], StringComparison.InvariantCultureIgnoreCase));
        Assert.Contains(nouns, x => x.Equals(parts[1], StringComparison.InvariantCultureIgnoreCase));
        Assert.Contains(verbs, x => x.Equals(parts[2], StringComparison.InvariantCultureIgnoreCase));
    }

    [Theory]
    [InlineData(Enums.CharacterGroup.Lowercase, 6)]
    [InlineData(Enums.CharacterGroup.Uppercase, 6)]
    [InlineData(Enums.CharacterGroup.Digits, 6)]
    [InlineData(Enums.CharacterGroup.Special, 6)]
    public void GivenCharcterGroup_AndLength_WhenGenerateRandomPassword_ThenReturnsPassword(
        Enums.CharacterGroup characterGroup,
        int length)
    {
        // Arrange
        var expectAllExistIn = CharacterGroups.GetGroup(characterGroup);

        // Arrange & Act
        var result = this.sut.GenerateRandomPassword(characterGroup, length);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Equal(length, result.Length);
        Assert.True(result.All(c => expectAllExistIn.Contains(c)));
    }

    public void Dispose()
    {
        this.sut.Dispose();
    }
}
