using Clypse.Core.Enums;

namespace Clypse.Portal.Models.Vaults;

/// <summary>
/// A field of a secret, as the editor shows it: the secret's property and how it is edited.
/// </summary>
public class SecretField
{
    /// <summary>
    /// Gets the property's name, which is also the key of its value in the secret's data.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets how the field is edited.
    /// </summary>
    public SecretFieldType FieldType { get; init; }

    /// <summary>
    /// Gets a value indicating whether the secret needs a value for it.
    /// </summary>
    public bool IsRequired { get; init; }
}
