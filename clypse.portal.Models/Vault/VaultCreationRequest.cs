namespace clypse.portal.Models.Vault;

/// <summary>
/// Represents a request to create a new vault.
/// </summary>
public class VaultCreationRequest
{
    /// <summary>
    /// Gets or sets the name of the vault.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the vault.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the passphrase used to encrypt the vault.
    /// </summary>
    public string Passphrase { get; set; } = string.Empty;
}
