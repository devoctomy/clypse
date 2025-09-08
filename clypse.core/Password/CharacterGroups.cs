using clypse.core.Enums;

namespace clypse.core.Password;

/// <summary>
/// Provides character groups for password generation.
/// </summary>
public static class CharacterGroups
{
    /// <summary>
    /// Gets the characters corresponding to the specified character group.
    /// </summary>
    /// <param name="group">The character group to retrieve characters for.</param>
    /// <returns>A string containing the characters of the specified group.</returns>
    /// <exception cref="NotImplementedException">Thrown if the character group is not implemented.</exception>
    public static string GetGroup(CharacterGroup group) => group switch
    {
        CharacterGroup.Lowercase => "abcdefghijklmnopqrstuvwxyz",
        CharacterGroup.Uppercase => "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
        CharacterGroup.Digits => "0123456789",
        CharacterGroup.Special => "!\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~",
        _ => throw new NotImplementedException($"Character group '{group}' is not implemented."),
    };

    /// <summary>
    /// Gets a list of individual character groups from a combined CharacterGroup flag.
    /// </summary>
    /// <param name="groups">The combined CharacterGroup flags.</param>
    /// <returns>A list of individual CharacterGroup values.</returns>
    public static List<CharacterGroup> GetGroupsFromFlags(CharacterGroup groups)
    {
        var foundGroups = new List<CharacterGroup>();

        var allGroups = Enum.GetValues<CharacterGroup>();
        foreach (var group in allGroups)
        {
            if (groups.HasFlag(group))
            {
                foundGroups.Add(group);
            }
        }

        return foundGroups;
    }
}
