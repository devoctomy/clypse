using clypse.core.Base;
using clypse.core.Cryptogtaphy;
using System.Text.Json.Serialization;

namespace clypse.core.Vault;


public class VaultInfo
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Base64Salt { get; set; }
    public VaultIndex Index { get; set; }

    public VaultInfo(
        string name,
        string description)
    {
        Id = Guid.NewGuid().ToString();
        Name = name;
        Description = description;
        var salt = CryptoHelpers.GenerateRandomBytes(16);
        Base64Salt = Convert.ToBase64String(salt);
        Index = new VaultIndex();
    }
}
