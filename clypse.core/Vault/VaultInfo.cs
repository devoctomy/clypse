using clypse.core.Cryptogtaphy;
using System.Text.Json.Serialization;

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
        Id = Guid.NewGuid().ToString();
        Name = name;
        Description = description;

        var salt = CryptoHelpers.Sha256HashString(Id);
        Base64Salt = Convert.ToBase64String(salt);
    }
}
