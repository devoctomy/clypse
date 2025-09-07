using clypse.core.Cryptogtaphy;
using clypse.core.Enums;
using clypse.core.Password;
using Moq;

namespace clypse.core.UnitTests.Password;

public class RandomStringTokenProcessorTests
{
    [Theory]
    [InlineData("randstr(foobar)", true)]
    [InlineData("randstr()", true)]
    [InlineData("randstr(", true)]
    [InlineData("randstr", false)]
    [InlineData("randomstring()", false)]
    public void GivenToken_WhenIsApplicable_ThenCorrectResultReturned(
            string token,
            bool expectedResult)
    {
        // Arrange
        var sut = new RandomStringTokenProcessor();

        // Act
        var result = sut.IsApplicable(token);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public void GivenToken_AndValidDictionary_WhenProcess_ThenDictionaryLoaded_AndRandomWordReturned()
    {
        // Arrange
        var token = "randstr(Foobar123,3)";
        var mockRandomGeneratorService = new Mock<IRandomGeneratorService>();
        var mockPasswordGeneratorService = new Mock<IPasswordGeneratorService>();
        var sut = new RandomStringTokenProcessor();
        var expectedWord = "HelloWorld";

        mockPasswordGeneratorService.SetupGet(
            x => x.RandomGeneratorService)
            .Returns(mockRandomGeneratorService.Object);

        mockRandomGeneratorService.Setup(x => x.GetRandomStringContainingCharacters(
            It.IsAny<int>(),
            It.IsAny<string>()))
            .Returns(expectedWord);

        // Act
        var result = sut.Process(mockPasswordGeneratorService.Object, token);

        // Assert
        Assert.Equal(expectedWord, result);

        mockRandomGeneratorService.Verify(
            x => x.GetRandomStringContainingCharacters(
            It.Is<int>(y => y == 3),
            It.Is<string>(y => y == "Foobar123")), Times.Once);
    }

    [Theory]
    [InlineData("randstr([lowercase],6)", CharacterGroup.Lowercase)]
    [InlineData("randstr([uppercase],6)", CharacterGroup.Uppercase)]
    [InlineData("randstr([digits],6)", CharacterGroup.Digits)]
    [InlineData("randstr([special],6)", CharacterGroup.Special)]
    public void GivenToken_WhenProcess_ThenExpectedLettersReturned(
        string token,
        CharacterGroup characterGroup)
    {
        // Arrange
        var mockRandomGeneratorService = new Mock<IRandomGeneratorService>();
        var mockPasswordGeneratorService = new Mock<IPasswordGeneratorService>();
        var charGroup = CharacterGroups.GetGroup(characterGroup);
        var sut = new RandomStringTokenProcessor();

        mockPasswordGeneratorService.SetupGet(
            x => x.RandomGeneratorService)
            .Returns(mockRandomGeneratorService.Object);

        mockRandomGeneratorService.Setup(x => x.GetRandomStringContainingCharacters(
            It.IsAny<int>(),
            It.IsAny<string>()))
            .Returns((int length, string chars) =>
            {
                return chars;
            });

        // Act
        var result = sut.Process(mockPasswordGeneratorService.Object, token);

        // Assert
        Assert.All(result, c => Assert.Contains(c, charGroup));
    }
}
