using clypse.core.Enums;

namespace clypse.core.Extensions;

/// <summary>
/// Extension methods for the CharacterGroup enum.
/// </summary>
public static class CharacterGroupExtensions
{
    /// <summary>
    /// Gets a list of individual character groups from a combined CharacterGroup flag.
    /// </summary>
    /// <param name="groups">The combined CharacterGroup flags.</param>
    /// <returns>A list of individual CharacterGroup values.</returns>
    public static List<CharacterGroup> GetGroupsFromFlags(this CharacterGroup groups)
    {
        var foundGroups = new List<CharacterGroup>();

        var allGroups = Enum.GetValues<CharacterGroup>();
        foreach (var group in allGroups)
        {
            if (group == CharacterGroup.None)
            {
                continue;
            }

            if (groups.HasFlag(group))
            {
                foundGroups.Add(group);
            }
        }

        return foundGroups;
    }
}
