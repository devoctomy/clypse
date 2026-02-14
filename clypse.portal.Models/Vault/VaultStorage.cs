using System.Text.Json.Serialization;

namespace clypse.portal.Models.Vault;

public class VaultStorage
{
    [JsonPropertyName("vaults")]
    public List<VaultMetadata> Vaults { get; set; } = new();
}
