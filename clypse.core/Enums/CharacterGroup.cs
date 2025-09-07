namespace clypse.core.Enums;

/// <summary>
/// Represents different character groups that can be used in password generation. 
/// </summary>
[Flags]
public enum CharacterGroup
{
    /// <summary>
    /// No specific dictionary type or default type.
    /// </summary>
    None = 0,

    /// <summary>
    /// Lowercase letters (a-z).
    /// </summary>
    Lowercase = 1,

    /// <summary>
    /// Uppercase letters (A-Z).
    /// </summary>
    Uppercase = 2,

    /// <summary>
    /// Digits (0-9).
    /// </summary>
    Digits = 4,

    /// <summary>
    /// Special characters (e.g., !, @, #, $, etc.).
    /// </summary>
    Special = 8,
}
