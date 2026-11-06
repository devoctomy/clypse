namespace Clypse.Portal.Application.Exceptions;

/// <summary>
/// The codes of <see cref="ClypseException"/>. Each has its text in <c>ClypseText.resx</c>, as <c>Error.{code}</c>.
/// </summary>
public static class ClypseErrorCodes
{
    /// <summary>
    /// A vault was given no name.
    /// </summary>
    public const string VaultNameRequired = "Clypse.VaultNameRequired";

    /// <summary>
    /// The vault no longer exists (e.g. it was deleted on another device).
    /// </summary>
    public const string VaultNotFound = "Clypse.VaultNotFound";

    /// <summary>
    /// A secret is missing required fields. Argument: the fields, comma separated.
    /// </summary>
    public const string SecretInvalid = "Clypse.SecretInvalid";

    /// <summary>
    /// A secret's file could not be read as a secret.
    /// </summary>
    public const string SecretUnreadable = "Clypse.SecretUnreadable";

    /// <summary>
    /// A secret's id is not a GUID, so it cannot name a file.
    /// </summary>
    public const string SecretIdInvalid = "Clypse.SecretIdInvalid";

    /// <summary>
    /// No vault is open.
    /// </summary>
    public const string NoVaultOpen = "Clypse.NoVaultOpen";

    /// <summary>
    /// An import file held no secrets.
    /// </summary>
    public const string ImportNothingFound = "Clypse.ImportNothingFound";

    /// <summary>
    /// An import file could not be read in the format chosen. Argument: the format's name.
    /// </summary>
    public const string ImportUnreadable = "Clypse.ImportUnreadable";
}
