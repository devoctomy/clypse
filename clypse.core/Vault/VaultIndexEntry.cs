namespace clypse.core.Vault;

/// <summary>
/// Represents a single entry in the vault index, containing metadata about a secret without the actual secret data.
/// </summary>
public class VaultIndexEntry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VaultIndexEntry"/> class with the specified metadata.
    /// </summary>
    /// <param name="id">The unique identifier of the secret.</param>
    /// <param name="name">The name of the secret.</param>
    /// <param name="description">The description of the secret.</param>
    /// <param name="tags">The comma-separated tags for the secret.</param>
    public VaultIndexEntry(
        string id,
        string name,
        string? description,
        string? tags)
    {
        this.Id = id;
        this.Name = name;
        this.Description = description;
        this.Tags = tags;
    }

    /// <summary>
    /// Gets or sets the unique identifier of the secret.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the secret.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the description of the secret.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the comma-separated tags for the secret.
    /// </summary>
    public string? Tags { get; set; }
}
