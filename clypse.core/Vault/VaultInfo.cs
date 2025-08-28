using System.Text.Json.Serialization;
using clypse.core.Cryptogtaphy;

namespace clypse.core.Vault;

/// <summary>
/// Contains basic information about a vault including its identity, name, description, and cryptographic salt.
/// </summary>
public class VaultInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VaultInfo"/> class with the specified name and description.
    /// </summary>
    /// <param name="name">The name of the vault.</param>
    /// <param name="description">The description of the vault.</param>
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

    /// <summary>
    /// Gets or sets the unique identifier of the vault.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the vault.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the description of the vault.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Gets or sets the base64-encoded cryptographic salt derived from the vault ID.
    /// </summary>
    [JsonIgnore]
    public string Base64Salt { get; set; }
}
