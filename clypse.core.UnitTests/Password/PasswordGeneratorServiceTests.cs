namespace clypse.core.UnitTests.Password;

public class PasswordGeneratorServiceTests
{
    [Fact]
    public void GivenDictionaryType_WhenLoadDictionary_ThenReturnsListOfWords()
    {
        // Arrange
        var sut = new core.Password.PasswordGeneratorService();
        var dictionaryType = Enums.DictionaryType.Adjective;

        // Act
        var result = sut.LoadDictionary(dictionaryType);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public void GivenTemplate_WhenGenerateMemorablePassword_ThenReturnsPassword()
    {
        // Arrange
        var sut = new core.Password.PasswordGeneratorService();
        var template = "{dict(adjective):upper}-{dict(noun):lower}-{dict(verb):upper}";
        var adjectives = sut.LoadDictionary(Enums.DictionaryType.Adjective);
        var nouns = sut.LoadDictionary(Enums.DictionaryType.Noun);
        var verbs = sut.LoadDictionary(Enums.DictionaryType.Verb);

        // Act
        var result = sut.GenerateMemorablePassword(template);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        var parts = result.Split('-');
        Assert.Contains(adjectives, x => x.Equals(parts[0], StringComparison.InvariantCultureIgnoreCase));
        Assert.Contains(nouns, x => x.Equals(parts[1], StringComparison.InvariantCultureIgnoreCase));
        Assert.Contains(verbs, x => x.Equals(parts[2], StringComparison.InvariantCultureIgnoreCase));
    }
}
