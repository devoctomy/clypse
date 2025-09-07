namespace clypse.core.Vault;

/// <summary>
/// Represents a listing of a vault, including its unique identifier and manifest.
/// </summary>
public class VaultListing
{
    /// <summary>
    /// Gets or sets the unique identifier for the vault.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the manifest for the vault.
    /// </summary>
    public VaultManifest? Manifest { get; set; }
}
