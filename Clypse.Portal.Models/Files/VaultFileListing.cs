namespace Clypse.Portal.Models.Files;

/// <summary>
/// What a vault's file store holds of its files, found without reading or decrypting anything.
/// </summary>
public class VaultFileListing
{
    /// <summary>
    /// Gets the ids of the metadata files: the vault's files and its empty folders' records.
    /// </summary>
    public IReadOnlyList<string> EntryIds { get; init; } = [];

    /// <summary>
    /// Gets the ids of the files whose content is on this device.
    /// </summary>
    public IReadOnlyList<string> ContentIds { get; init; } = [];
}
