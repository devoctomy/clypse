using clypse.core.Enums;

namespace clypse.core.Secrets;

/// <summary>
/// Indicates that a property is a field within a secret and specifies the type of field it is.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class SecretFieldAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the display order of the secret field.
    /// </summary>
    public int DisplayOrder { get; set; } = -1;

    /// <summary>
    /// Gets or sets the type of the secret field.
    /// </summary>
    public SecretFieldType FieldType { get; set; } = SecretFieldType.None;
}
