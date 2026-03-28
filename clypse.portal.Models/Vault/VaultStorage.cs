using System.Text.Json.Serialization;

namespace clypse.portal.Models.Vault;

/// <summary>
/// Represents the storage container for vault metadata.
/// </summary>
public class VaultStorage
{
    /// <summary>
    /// Gets or sets the list of vault metadata entries.
    /// </summary>
    [JsonPropertyName("vaults")]
    public List<VaultMetadata> Vaults { get; set; } = [];
}
