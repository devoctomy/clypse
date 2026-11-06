using Clypse.Core.Cryptography;
using Clypse.Core.Data;
using Clypse.Core.Enums;
using Clypse.Core.Password;
using Moq;
using System.Reflection;

namespace Clypse.Core.UnitTests.Password;

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
        var mockEmbeddedResourceLoaderService = new Mock<IEmbeddedResourceLoaderService>();
        var sut = new DictionaryTokenProcessor(mockEmbeddedResourceLoaderService.Object);

        // Act
        var result = sut.IsApplicable(token);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public async Task GivenToken_AndValidDictionary_WhenProcess_ThenDictionaryLoaded_AndRandomWordReturned()
    {
        // Arrange
        var token = "dict(verb)";
        var mockEmbeddedResourceLoaderService = new Mock<IEmbeddedResourceLoaderService>();
        var mockRandomGeneratorService = new Mock<IRandomGeneratorService>();
        var mockPasswordGeneratorService = new Mock<IPasswordGeneratorService>();
        var sut = new DictionaryTokenProcessor(mockEmbeddedResourceLoaderService.Object);

        var words = new List<string>
        {
            "word1",
            "word2",
            "word3",
        };
        var expectedWord = words[1];

        mockEmbeddedResourceLoaderService.Setup(
            x => x.LoadHashSetAsync(
            It.Is<string>(y => y == ResourceKeys.VerbResourceKey),
            It.IsAny<Assembly?>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync([.. words]);

        mockPasswordGeneratorService.SetupGet(
            x => x.RandomGeneratorService)
            .Returns(mockRandomGeneratorService.Object);

        mockRandomGeneratorService.Setup(
            x => x.GetRandomArrayEntry<string>(
            It.IsAny<string[]>()))
            .Returns(expectedWord);

        // Act
        var result = await sut.ProcessAsync(
            mockPasswordGeneratorService.Object,
            token,
            CancellationToken.None);

        // Assert
        Assert.Equal(expectedWord, result);

        mockEmbeddedResourceLoaderService.Verify(
            x => x.LoadHashSetAsync(
            It.Is<string>(y => y == ResourceKeys.VerbResourceKey),
            It.IsAny<Assembly?>(),
            It.IsAny<CancellationToken>()), Times.Once);
        mockRandomGeneratorService.Verify(
            x => x.GetRandomArrayEntry<string>(
            It.Is<string[]>(y => y.SequenceEqual(words))), Times.Once);
    }

    [Fact]
    public async Task GivenToken_AndInvalidDictionary_WhenProcess_ThenEmptyStringReturned()
    {
        // Arrange
        var token = "dict(foo)";
        var mockEmbeddedResourceLoaderService = new Mock<IEmbeddedResourceLoaderService>();
        var mockPasswordGeneratorService = new Mock<IPasswordGeneratorService>();
        var sut = new DictionaryTokenProcessor(mockEmbeddedResourceLoaderService.Object);

        // Act
        var result = await sut.ProcessAsync(
            mockPasswordGeneratorService.Object,
            token,
            CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GivenToken_AndMultipleDictionaries_WhenProcess_ThenDictionaryLoaded_AndRandomWordReturned()
    {
        // Arrange
        var token = $"dict({DictionaryType.Verb.ToString().ToLower()}|{DictionaryType.Adjective.ToString().ToLower()}|{DictionaryType.Noun.ToString().ToLower()})";
        using var randomGeneratorService = new RandomGeneratorService();
        var mockEmbeddedResourceLoaderService = new Mock<IEmbeddedResourceLoaderService>();
        var mockRandomGeneratorService = new Mock<IRandomGeneratorService>();
        var mockPasswordGeneratorService = new Mock<IPasswordGeneratorService>();
        var sut = new DictionaryTokenProcessor(mockEmbeddedResourceLoaderService.Object);

        var words = new List<string>
        {
            "verb",
            "adjective",
            "noun",
        };
        var expectedWord = words[1];

        mockEmbeddedResourceLoaderService.Setup(
            x => x.LoadHashSetAsync(
                It.Is<string>(y => y == ResourceKeys.VerbResourceKey),
                It.IsAny<Assembly?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(["verb"]);

        mockEmbeddedResourceLoaderService.Setup(
            x => x.LoadHashSetAsync(
                It.Is<string>(y => y == ResourceKeys.AdjectivesResourceKey),
                It.IsAny<Assembly?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(["adjective"]);

        mockEmbeddedResourceLoaderService.Setup(
            x => x.LoadHashSetAsync(
                It.Is<string>(y => y == ResourceKeys.NounResourceKey),
                It.IsAny<Assembly?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(["noun"]);

        mockPasswordGeneratorService.SetupGet(
            x => x.RandomGeneratorService)
            .Returns(mockRandomGeneratorService.Object);

        mockRandomGeneratorService.Setup(
            x => x.GetRandomArrayEntry<string>(
            It.IsAny<string[]>()))
            .Returns((string[] array) =>
            {
                var randomEntry = randomGeneratorService.GetRandomArrayEntry<string>(array);
                return randomEntry;
            });

        // Act
        var result = await sut.ProcessAsync(
            mockPasswordGeneratorService.Object,
            token,
            CancellationToken.None);

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains(words, x => x.Equals(result, StringComparison.InvariantCultureIgnoreCase));
    }
}
