using System.Text.Json.Serialization;
using clypse.core.Vault;

namespace clypse.portal.Models.Vault;

/// <summary>
/// Represents metadata for a vault.
/// </summary>
public class VaultMetadata
{
    /// <summary>
    /// Gets or sets the unique identifier of the vault.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the vault.
    /// </summary>
    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the description of the vault.
    /// </summary>
    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }

    /// <summary>
    /// Transient property containing the decrypted vault index entries.
    /// This is not persisted to storage for security reasons.
    /// </summary>
    [JsonIgnore]
    public List<VaultIndexEntry>? IndexEntries { get; set; }
}