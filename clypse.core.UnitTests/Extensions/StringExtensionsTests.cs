using clypse.core.Enums;
using clypse.core.Extensions;

namespace clypse.core.UnitTests.Extensions;

public class StringExtensionsTests
{
    [Theory]
    [InlineData("Password123!", CharacterGroup.Lowercase, true)]
    [InlineData("Password123!", CharacterGroup.Uppercase, true)]
    [InlineData("Password123!", CharacterGroup.Digits, true)]
    [InlineData("Password123!", CharacterGroup.Special, true)]
    [InlineData("PASSWORD123!", CharacterGroup.Lowercase, false)]
    [InlineData("password123!", CharacterGroup.Uppercase, false)]
    [InlineData("Password!", CharacterGroup.Digits, false)]
    [InlineData("password123", CharacterGroup.Special, false)]
    public void GivenInputString_AndCharacterGroup_WhenContainsCharactersFromGroupIsCalled_ThenReturnsExpectedResult(
        string input,
        CharacterGroup characterGroup,
        bool expectedResult)
    {
        // Arrange & Act
        var result = input.ContainsCharactersFromGroup(characterGroup);

        // Assert
        Assert.Equal(expectedResult, result);
    }
}
