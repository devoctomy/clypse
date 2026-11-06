using Clypse.Core.Enums;

namespace Clypse.Portal.Models.Settings;

/// <summary>
/// What the password generator starts with, and the user's own memorable templates: kept on the device.
/// </summary>
public class PasswordGeneratorPreferences
{
    /// <summary>
    /// The default length of a random password.
    /// </summary>
    public const int DefaultLength = 20;

    /// <summary>
    /// Gets or sets a value indicating whether the generator starts with memorable passwords, rather than random ones.
    /// </summary>
    public bool IsMemorable { get; set; }

    /// <summary>
    /// Gets or sets a random password's length.
    /// </summary>
    public int Length { get; set; } = DefaultLength;

    /// <summary>
    /// Gets or sets the kinds of character in a random password.
    /// </summary>
    public CharacterGroup Groups { get; set; } = CharacterGroup.Lowercase | CharacterGroup.Uppercase | CharacterGroup.Digits | CharacterGroup.Special;

    /// <summary>
    /// Gets or sets a value indicating whether a random password has at least one character of each kind.
    /// </summary>
    public bool AtLeastOneOfEach { get; set; } = true;

    /// <summary>
    /// Gets or sets the name of the memorable template the generator starts with, or null for the first.
    /// </summary>
    public string? Template { get; set; }

    /// <summary>
    /// Gets or sets the user's own memorable templates, offered after the built in ones.
    /// </summary>
    public List<MemorablePasswordTemplate> UserTemplates { get; set; } = [];
}
