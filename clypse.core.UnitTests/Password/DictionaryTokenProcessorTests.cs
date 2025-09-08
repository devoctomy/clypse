using clypse.core.Cryptogtaphy;
using clypse.core.Enums;
using clypse.core.Password;
using Moq;

namespace clypse.core.UnitTests.Password;

public class DictionaryTokenProcessorTests
{
    [Theory]
    [InlineData("dict(foobar)", true)]
    [InlineData("dict()", true)]
    [InlineData("dict(", true)]
    [InlineData("dict", false)]
    [InlineData("dictionary()", false)]
    public void GivenToken_WhenIsApplicable_ThenCorrectResultReturned(
        string token,
        bool expectedResult)
    {
        // Arrange
        var sut = new DictionaryTokenProcessor();

        // Act
        var result = sut.IsApplicable(token);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public void GivenToken_AndValidDictionary_WhenProcess_ThenDictionaryLoaded_AndRandomWordReturned()
    {
        // Arrange
        var token = "dict(verb)";
        var mockRandomGeneratorService = new Mock<IRandomGeneratorService>();
        var mockPasswordGeneratorService = new Mock<IPasswordGeneratorService>();
        var sut = new DictionaryTokenProcessor();

        var words = new List<string>
        {
            "word1",
            "word2",
            "word3",
        };
        var expectedWord = words[1];

        mockPasswordGeneratorService.Setup(
            x => x.GetOrLoadDictionary(
            It.IsAny<Enums.DictionaryType>()))
            .Returns(words);

        mockPasswordGeneratorService.SetupGet(
            x => x.RandomGeneratorService)
            .Returns(mockRandomGeneratorService.Object);

        mockRandomGeneratorService.Setup(
            x => x.GetRandomArrayEntry<string>(
            It.IsAny<string[]>()))
            .Returns(expectedWord);

        // Act
        var result = sut.Process(mockPasswordGeneratorService.Object, token);

        // Assert
        Assert.Equal(expectedWord, result);

        mockPasswordGeneratorService.Verify(
            x => x.GetOrLoadDictionary(
            It.Is<DictionaryType>(y => y == DictionaryType.Verb)), Times.Once);
        mockRandomGeneratorService.Verify(
            x => x.GetRandomArrayEntry<string>(
            It.Is<string[]>(y => y.SequenceEqual(words))), Times.Once);
    }

    [Fact]
    public void GivenToken_AndInvalidDictionary_WhenProcess_ThenDictionaryLoaded_AndRandomWordReturned()
    {
        // Arrange
        var token = "dict(foo)";
        var mockRandomGeneratorService = new Mock<IRandomGeneratorService>();
        var mockPasswordGeneratorService = new Mock<IPasswordGeneratorService>();
        var sut = new DictionaryTokenProcessor();

        var words = new List<string>
        {
            "word1",
            "word2",
            "word3",
        };
        var expectedWord = words[1];

        mockPasswordGeneratorService.Setup(
            x => x.GetOrLoadDictionary(
            It.IsAny<Enums.DictionaryType>()))
            .Returns(words);

        // Act
        var result = sut.Process(mockPasswordGeneratorService.Object, token);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GivenToken_AndMultipleDictionaries_WhenProcess_ThenDictionaryLoaded_AndRandomWordReturned()
    {
        // Arrange
        var token = $"dict({DictionaryType.Verb.ToString().ToLower()}|{DictionaryType.Adjective.ToString().ToLower()}|{DictionaryType.Noun.ToString().ToLower()})";
        using var randomGeneratorService = new RandomGeneratorService();
        var mockRandomGeneratorService = new Mock<IRandomGeneratorService>();
        var mockPasswordGeneratorService = new Mock<IPasswordGeneratorService>();
        var sut = new DictionaryTokenProcessor();

        var words = new List<string>
        {
            "verb",
            "adjective",
            "noun",
        };
        var expectedWord = words[1];

        mockPasswordGeneratorService.Setup(
            x => x.GetOrLoadDictionary(
            It.IsAny<DictionaryType>()))
            .Returns((DictionaryType dictType) =>
            {
                return [dictType.ToString()];
            });

        mockPasswordGeneratorService.SetupGet(
            x => x.RandomGeneratorService)
            .Returns(mockRandomGeneratorService.Object);

        mockRandomGeneratorService.Setup(
            x => x.GetRandomArrayEntry<string>(
            It.IsAny<string[]>()))
            .Returns((string[] array) =>
            {
                return randomGeneratorService.GetRandomArrayEntry<string>(array);
            });

        // Act
        var result = sut.Process(mockPasswordGeneratorService.Object, token);

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains(words, x => x.Equals(result, StringComparison.InvariantCultureIgnoreCase));
    }
}
