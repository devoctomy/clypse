namespace clypse.core.Enums;

/// <summary>
/// Types of fields that a secret may contain.
/// </summary>
public enum SecretFieldType
{
    /// <summary>
    /// No specific field type.
    /// </summary>
    None = 0,

    /// <summary>
    /// Single line text field.
    /// </summary>
    SingleLineText = 1,

    /// <summary>
    /// Multi line text field.
    /// </summary>
    MultiLineText = 2,

    /// <summary>
    /// Tag list field.
    /// </summary>
    TagList = 3,

    /// <summary>
    /// Password field.
    /// </summary>
    Password = 4,
}
