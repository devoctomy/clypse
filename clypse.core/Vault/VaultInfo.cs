using System.Text.Json.Serialization;
using clypse.core.Cryptogtaphy;

namespace clypse.core.Vault;

public class VaultInfo
{
    public string Id { get; set; }

    public string Name { get; set; }

    public string Description { get; set; }

    [JsonIgnore]
    public string Base64Salt { get; set; }

    public VaultInfo(
        string name,
        string description)
    {
        this.Id = Guid.NewGuid().ToString();
        this.Name = name;
        this.Description = description;

        var salt = CryptoHelpers.Sha256HashString(this.Id);
        this.Base64Salt = Convert.ToBase64String(salt);
    }
}
