namespace Clypse.Portal.Models.Files;

/// <summary>
/// The metadata of one file in a vault, or the record of one empty folder: kept, encrypted like a secret, as
/// <c>files/meta/{id}.json</c> under the vault's root. A file's content is kept apart from it, as
/// <c>files/blobs/{id}.bin</c>.
/// </summary>
public class VaultFileEntry
{
    /// <summary>
    /// Gets or sets the id (a GUID). It names the metadata file and, for a file, its content.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this is a file or the record of an empty folder.
    /// </summary>
    public VaultFileKind Kind { get; set; }

    /// <summary>
    /// Gets or sets a file's name, e.g. <c>Passport.jpg</c>. A folder record has none: its name is the end of
    /// <see cref="Folder"/>.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets a folder's path: its names joined by <c>/</c>. For a file, the folder it is in (empty at the top
    /// level). For a folder record, the empty folder the record keeps.
    /// </summary>
    public string Folder { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a file's content type, e.g. <c>image/jpeg</c>.
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// Gets or sets the size of a file's content, in bytes.
    /// </summary>
    public long? Size { get; set; }

    /// <summary>
    /// Gets or sets the user's notes on a file.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets when it was added.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets when it last changed.
    /// </summary>
    public DateTimeOffset LastUpdatedAt { get; set; }
}
