using clypse.core.Cryptogtaphy;
using clypse.core.Data;
using clypse.core.Extensions;
using clypse.core.Password;
using Moq;

namespace clypse.core.UnitTests.Password;

public class StandardWesternPasswordGeneratorServiceTests : IDisposable
{
    private readonly RandomGeneratorService randomGeneratorService;
    private readonly Mock<IDictionaryLoaderService> mockDictionaryLoaderService;
    private readonly List<IPasswordGeneratorTokenProcessor> tokenProcessors;
    private readonly StandardWesternPasswordGeneratorService sut;

    public StandardWesternPasswordGeneratorServiceTests()
    {
        this.randomGeneratorService = new RandomGeneratorService();
        this.mockDictionaryLoaderService = new Mock<IDictionaryLoaderService>();
        this.tokenProcessors =
        [
            new DictionaryTokenProcessor(this.mockDictionaryLoaderService.Object),
            new RandomStringTokenProcessor(),
        ];
        this.sut = new StandardWesternPasswordGeneratorService(
            this.randomGeneratorService,
            this.tokenProcessors);
    }

    [Theory]
    [InlineData("{randstr(abcdefg)}")]
    public async Task GivenInvalidTemplate_WhenLoadDictionary_ThenEmptyStringReturned(string invalidTemplate)
    {
        // Arrange & Act
        var result = await this.sut.GenerateMemorablePasswordAsync(
            invalidTemplate,
            false,
            CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GivenTemplateWithDictionarySelections_WhenGenerateMemorablePassword_ThenReturnsPassword()
    {
        // Arrange
        var template = "{dict(adjective):upper}-{dict(noun):lower}-{dict(verb):upper}";

        this.mockDictionaryLoaderService.Setup(x => x.LoadDictionaryAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((string name, CancellationToken ct) =>
            {
                return [name.Split('.')[0]];
            });

        // Act
        var result = await this.sut.GenerateMemorablePasswordAsync(
            template,
            false,
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        var parts = result.Split('-');
        Assert.Equal("ADJECTIVE", parts[0]);
        Assert.Equal("noun", parts[1]);
        Assert.Equal("VERB", parts[2]);
    }

    [Theory]
    [InlineData("{dict(adjective):random}{randstr(0123456789,6):random}{dict(verb):random}{randstr(-=_,1)}{dict(noun):random}", true, null)]
    [InlineData("{randstr(0,1)}{randstr(1,1)}{randstr(2,1)}{randstr(3,1)}{randstr(4,1)}{randstr(5,1)}{randstr(6,1)}{randstr(7,1)}{randstr(8,1)}{randstr(9,1)}", true, "0123456789")]
    public async Task GivenTemplate_AndShuffleTokens_WhenGenerateMemorablePassword_ThenPasswordReturned(
        string template,
        bool shuffleTokens,
        string? notEqualTo)
    {
        // Arrange
        this.mockDictionaryLoaderService.Setup(x => x.LoadDictionaryAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((string name, CancellationToken ct) =>
            {
                return [name.Split('.')[0]];
            });

        // Act
        var result = await this.sut.GenerateMemorablePasswordAsync(
            template,
            shuffleTokens,
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        if (!string.IsNullOrEmpty(notEqualTo))
        {
            Assert.NotEqual(notEqualTo, result);
        }
    }

    [Theory]
    [InlineData(Enums.CharacterGroup.Lowercase, 6)]
    [InlineData(Enums.CharacterGroup.Uppercase, 6)]
    [InlineData(Enums.CharacterGroup.Digits, 6)]
    [InlineData(Enums.CharacterGroup.Special, 6)]
    public void GivenCharcterGroup_AndLength_AndNotOneFromEachGroup_WhenGenerateRandomPassword_ThenReturnsPassword(
        Enums.CharacterGroup characterGroup,
        int length)
    {
        // Arrange
        var expectAllExistIn = CharacterGroups.GetGroup(characterGroup);

        // Arrange & Act
        var result = this.sut.GenerateRandomPassword(characterGroup, length, false);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Equal(length, result.Length);
        Assert.True(result.All(c => expectAllExistIn.Contains(c)));
    }

    [Theory]
    [InlineData(Enums.CharacterGroup.Lowercase, 1)]
    [InlineData(Enums.CharacterGroup.Lowercase, 16)]
    [InlineData(Enums.CharacterGroup.Lowercase | Enums.CharacterGroup.Uppercase, 2)]
    [InlineData(Enums.CharacterGroup.Lowercase | Enums.CharacterGroup.Uppercase, 16)]
    [InlineData(Enums.CharacterGroup.Lowercase | Enums.CharacterGroup.Uppercase | Enums.CharacterGroup.Digits, 3)]
    [InlineData(Enums.CharacterGroup.Lowercase | Enums.CharacterGroup.Uppercase | Enums.CharacterGroup.Digits, 16)]
    [InlineData(Enums.CharacterGroup.Lowercase | Enums.CharacterGroup.Uppercase | Enums.CharacterGroup.Digits | Enums.CharacterGroup.Special, 4)]
    [InlineData(Enums.CharacterGroup.Lowercase | Enums.CharacterGroup.Uppercase | Enums.CharacterGroup.Digits | Enums.CharacterGroup.Special, 16)]
    public void GivenCharacterGroups_AndLength_WhenGenerateRandomPassword_ThenReturnsPassword(
        Enums.CharacterGroup characterGroups,
        int length)
    {
        // Arrange
        var groupsFromFlags = characterGroups.GetGroupsFromFlags();

        // Act
        var result = this.sut.GenerateRandomPassword(characterGroups, length, true);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Equal(length, result.Length);

        foreach (var curGroup in groupsFromFlags)
        {
            Assert.True(result.ContainsCharactersFromGroup(curGroup));
        }
    }

    [Fact]
    public void GivenNone_WhenGenerateRandomPassword_ThenEmptyStringReturned()
    {
        // Arrange & Act
        var result = this.sut.GenerateRandomPassword(Enums.CharacterGroup.None, 16, true);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    public void Dispose()
    {
        this.sut.Dispose();
    }
}
