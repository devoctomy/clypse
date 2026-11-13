using Clypse.Core.Enums;
using Clypse.Core.Password;

namespace Clypse.Core.UnitTests.Password;

public class CharacterGroupsTests
{
    [Theory]
    [InlineData(CharacterGroup.Lowercase, "abcdefghijklmnopqrstuvwxyz")]
    [InlineData(CharacterGroup.Uppercase, "ABCDEFGHIJKLMNOPQRSTUVWXYZ")]
    [InlineData(CharacterGroup.Digits, "0123456789")]
    [InlineData(CharacterGroup.Special, "!\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~")]
    public void GivenASingleGroup_WhenGetGroup_ThenItsCharactersAreReturned(CharacterGroup group, string expected)
    {
        // Act
        var result = CharacterGroups.GetGroup(group);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(CharacterGroup.None)]
    [InlineData(CharacterGroup.Lowercase | CharacterGroup.Digits)]
    public void GivenNoneOrCombinedGroups_WhenGetGroup_ThenNotImplementedExceptionThrown(CharacterGroup group)
    {
        // Act & Assert
        Assert.Throws<NotImplementedException>(() => CharacterGroups.GetGroup(group));
    }
}
