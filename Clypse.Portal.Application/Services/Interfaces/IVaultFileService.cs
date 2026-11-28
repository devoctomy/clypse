using Clypse.Portal.Models.Files;

namespace Clypse.Portal.Application.Services.Interfaces;

/// <summary>
/// One vault's files. Each file is two encrypted files under the vault's root: its metadata,
/// <c>files/meta/{id}.json</c>, and its content, <c>files/blobs/{id}.bin</c>. An empty folder is a metadata file alone.
/// Files are named only by a GUID, so nothing about a file (its name, its type, its folder) shows in a path. Created
/// for a vault by <see cref="IVaultFileServiceFactory"/>.
/// </summary>
public interface IVaultFileService
{
    /// <summary>
    /// Lists the ids of the vault's metadata files, and of the files whose content is on this device, without reading
    /// or decrypting anything.
    /// </summary>
    /// <param name="ct">A token to cancel with.</param>
    /// <returns>The ids.</returns>
    Task<VaultFileListing> ListAsync(CancellationToken ct = default);

    /// <summary>
    /// Reads the metadata of a file, or an empty folder's record. The content is not read.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <param name="ct">A token to cancel with.</param>
    /// <returns>The metadata, or null if there is none with that id.</returns>
    Task<VaultFileEntry?> GetAsync(string id, CancellationToken ct = default);

    /// <summary>
    /// Adds a file: its content is written first, then its metadata.
    /// </summary>
    /// <param name="name">The file's name.</param>
    /// <param name="folder">The path of the folder it goes in, empty for the top level.</param>
    /// <param name="contentType">The file's content type.</param>
    /// <param name="content">The file's content.</param>
    /// <param name="ct">A token to cancel with.</param>
    /// <returns>The new file's metadata.</returns>
    Task<VaultFileEntry> AddFileAsync(string name, string? folder, string? contentType, byte[] content, CancellationToken ct = default);

    /// <summary>
    /// Adds the record of an empty folder.
    /// </summary>
    /// <param name="folder">The folder's path.</param>
    /// <param name="ct">A token to cancel with.</param>
    /// <returns>The record.</returns>
    Task<VaultFileEntry> AddFolderAsync(string folder, CancellationToken ct = default);

    /// <summary>
    /// Saves changed metadata (e.g. a new name, folder or notes). Its last updated time is set to now. The content is
    /// not touched.
    /// </summary>
    /// <param name="entry">The metadata.</param>
    /// <param name="ct">A token to cancel with.</param>
    /// <returns>The saved metadata.</returns>
    Task<VaultFileEntry> UpdateAsync(VaultFileEntry entry, CancellationToken ct = default);

    /// <summary>
    /// Deletes a file (its metadata first, then its content) or an empty folder's record, which has no content.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <param name="kind">Whether it is a file or a folder record.</param>
    /// <param name="ct">A token to cancel with.</param>
    /// <returns>A task that completes when it is deleted.</returns>
    Task DeleteAsync(string id, VaultFileKind kind = VaultFileKind.File, CancellationToken ct = default);

    /// <summary>
    /// Reads and decrypts a file's content.
    /// </summary>
    /// <param name="id">The file's id.</param>
    /// <param name="ct">A token to cancel with.</param>
    /// <returns>The content, or null if it is not on this device.</returns>
    Task<byte[]?> ReadContentAsync(string id, CancellationToken ct = default);

    /// <summary>
    /// Replaces a file's content: the content is written first, then its metadata, with the new size and time.
    /// </summary>
    /// <param name="id">The file's id.</param>
    /// <param name="content">The new content.</param>
    /// <param name="ct">A token to cancel with.</param>
    /// <returns>The file's metadata, as saved.</returns>
    Task<VaultFileEntry> SaveContentAsync(string id, byte[] content, CancellationToken ct = default);

    /// <summary>
    /// Gets the id of the metadata file a key names, if it is one of this vault's.
    /// </summary>
    /// <param name="key">A file key.</param>
    /// <returns>The id, or null if the key is not a metadata file of this vault.</returns>
    string? GetEntryId(string key);

    /// <summary>
    /// Gets the id of the file whose content a key names, if it is one of this vault's.
    /// </summary>
    /// <param name="key">A file key.</param>
    /// <returns>The id, or null if the key is not a file's content in this vault.</returns>
    string? GetContentId(string key);

    /// <summary>
    /// Gets the key of a file's content: the synced file whose versions are the file's history.
    /// </summary>
    /// <param name="id">The file's id.</param>
    /// <returns>The key.</returns>
    string GetContentKey(string id);
}
