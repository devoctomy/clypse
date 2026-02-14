using System.Text.Json.Serialization;
using clypse.core.Vault;

namespace clypse.portal.Models.Vault;

public class VaultMetadata
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; set; }
    
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