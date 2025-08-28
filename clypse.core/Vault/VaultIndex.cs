using clypse.core.Secrets;
using System;

namespace clypse.core.Vault;

/// <summary>
/// Represents an index of secrets within a vault, containing metadata about each secret for efficient lookup and management.
/// </summary>
public class VaultIndex
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VaultIndex"/> class with current timestamps.
    /// </summary>
    public VaultIndex()
    {
        this.Entries = [];
        this.CreatedAt = DateTime.UtcNow;
        this.LastUpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets or sets the list of vault index entries containing metadata about secrets.
    /// </summary>
    public List<VaultIndexEntry> Entries { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this index was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this index was last updated.
    /// </summary>
    public DateTime LastUpdatedAt { get; set; }
}
