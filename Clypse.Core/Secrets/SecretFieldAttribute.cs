using Clypse.Core.Enums;

namespace Clypse.Core.Secrets;

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

    /// <summary>
    /// Gets or sets a value indicating whether the field's value is itself a secret, such as an access key: hidden until
    /// asked for, and cleared from the clipboard after it is copied. Password fields always are.
    /// </summary>
    public bool IsSensitive { get; set; }

    /// <summary>
    /// Gets a value indicating whether the field's value is itself a secret: a password, or a field marked
    /// <see cref="IsSensitive"/>.
    /// </summary>
    public bool IsSecretValue => IsSensitive || FieldType == SecretFieldType.Password;
}
