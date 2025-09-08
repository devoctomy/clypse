using clypse.core.Enums;
using clypse.core.Password;

namespace clypse.core.Extensions;

/// <summary>
/// Extension methods for the <see cref="string"/> class.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Determines if the input string contains at least one character from the specified character group.
    /// </summary>
    /// <param name="input">The input string to check.</param>
    /// <param name="characterGroup">The character group to check against.</param>
    /// <returns>True if the input contains at least one character from the specified group; otherwise, false.</returns>
    public static bool ContainsCharactersFromGroup(
        this string input,
        CharacterGroup characterGroup)
    {
        var characterGroupChars = CharacterGroups.GetGroup(characterGroup);
        foreach (var c in input)
        {
            if (characterGroupChars.Contains(c))
            {
                return true;
            }
        }

        return false;
    }
}
