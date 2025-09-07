using clypse.core.Cryptogtaphy;
using clypse.core.Password;

namespace clypse.core.UnitTests.Password;

public class PasswordGeneratorServiceTests : IDisposable
{
    private readonly RandomGeneratorService randomGeneratorService;
    private readonly PasswordGeneratorService sut;

    public PasswordGeneratorServiceTests()
    {
        this.randomGeneratorService = new RandomGeneratorService();
        this.sut = new core.Password.PasswordGeneratorService(this.randomGeneratorService);
    }

    [Fact]
    public void GivenDictionaryType_WhenLoadDictionary_ThenReturnsListOfWords()
    {
        // Arrange
        var dictionaryType = Enums.DictionaryType.Adjective;

        // Act
        var result = this.sut.LoadDictionary(dictionaryType);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public void GivenTemplateWithDictionarySelections_WhenGenerateMemorablePassword_ThenReturnsPassword()
    {
        // Arrange
        var template = "{dict(adjective):upper}-{dict(noun):lower}-{dict(verb):upper}";
        var adjectives = this.sut.LoadDictionary(Enums.DictionaryType.Adjective);
        var nouns = this.sut.LoadDictionary(Enums.DictionaryType.Noun);
        var verbs = this.sut.LoadDictionary(Enums.DictionaryType.Verb);

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

    [Fact]
    public void GivenTemplateWithRandStrSection__WhenGenerateMemorablePassword_ThenReturnsPassword()
    {
        // Arrange
        var template = "{randstr(abcdefghijklmnopqrstuvwxyz,8):upper}-{randstr(abcdefghijklmnopqrstuvwxyz,8):lower}";

        // Act
        var result = this.sut.GenerateMemorablePassword(template);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        var parts = result.Split('-');
        Assert.Matches("^[A-Z]{8}$", parts[0]);
        Assert.Matches("^[a-z]{8}$", parts[1]);
    }

    public void Dispose()
    {
        this.sut.Dispose();
    }
}
