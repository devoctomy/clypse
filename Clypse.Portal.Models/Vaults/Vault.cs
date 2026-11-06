namespace Clypse.Portal.Models.Vaults;

/// <summary>
/// A vault: a protected sync profile, with its own passphrase and keys, whose secrets are the files under its root.
/// </summary>
public class Vault
{
    /// <summary>
    /// Gets or sets the vault's id (a GUID). It names the sync profile and its keys.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the key every file of the vault is under, e.g. <c>profiles/{id}/</c>.
    /// </summary>
    public string Root { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name it is shown by: the name the user gave it once it has been unlocked on this device, otherwise
    /// its id.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the vault's description, known only while it is unlocked.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether it is unlocked.
    /// </summary>
    public bool IsUnlocked { get; set; }
}
