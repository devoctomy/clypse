using Clypse.Portal.Models.Files;
using Clypse.Portal.Models.Vaults;

namespace Clypse.Portal.Application.Services.Interfaces;

/// <summary>
/// The open vault's files, held in memory while it is unlocked: their metadata only, never their content. The metadata
/// is read and decrypted once the vault is unlocked, in the same way its secrets are (<see cref="IVaultSession"/>).
/// Locking the vault (by hand, on idle, or in another tab) clears it. Changes synced from another device are applied as
/// they arrive.
/// </summary>
/// <remarks>
/// Folders are virtual. A file's metadata holds the path of the folder it is in, so a folder with a file in it needs no
/// record. An empty folder is kept by a record: the session removes a folder's record once a file is in it, and writes
/// one when the last thing in a folder is deleted or moved out, so the folder stays.
/// </remarks>
public interface IVaultFileSession
{
    /// <summary>
    /// Raised when anything the session holds changes: the vault, its files' metadata, or the progress of reading it.
    /// </summary>
    event EventHandler? Changed;

    /// <summary>
    /// Gets the open vault, or null if none is open.
    /// </summary>
    Vault? Vault { get; }

    /// <summary>
    /// Gets the metadata read so far: the vault's files, and the records of its empty folders.
    /// </summary>
    IReadOnlyList<VaultFileEntry> Entries { get; }

    /// <summary>
    /// Gets a value indicating whether the metadata is being read.
    /// </summary>
    bool IsLoading { get; }

    /// <summary>
    /// Gets how many of the vault's metadata files have been read so far.
    /// </summary>
    int LoadedCount { get; }

    /// <summary>
    /// Gets how many metadata files the vault has.
    /// </summary>
    int TotalCount { get; }

    /// <summary>
    /// Gets how many metadata files could not be read.
    /// </summary>
    int UnreadableCount { get; }

    /// <summary>
    /// Gets a value indicating whether a vault is open and unlocked. When it locks, everything decrypted is forgotten.
    /// </summary>
    bool IsUnlocked { get; }

    /// <summary>
    /// Checks whether a file's content is on this device. During a sync, a file's metadata can arrive before its content.
    /// </summary>
    /// <param name="id">The file's id.</param>
    /// <returns>True if its content is here.</returns>
    bool HasContent(string id);

    /// <summary>
    /// Opens a vault. If it is unlocked, its files' metadata is read; otherwise it is read once the vault is unlocked.
    /// </summary>
    /// <param name="vault">The vault.</param>
    /// <param name="ct">A token to cancel with.</param>
    /// <returns>A task that completes when the metadata has been read (or straight away, if the vault is locked).</returns>
    Task OpenAsync(Vault vault, CancellationToken ct = default);

    /// <summary>
    /// Closes the open vault, forgetting its files' metadata. The vault is not locked.
    /// </summary>
    void Close();

    /// <summary>
    /// Adds a file to the open vault.
    /// </summary>
    /// <param name="name">The file's name.</param>
    /// <param name="folder">The path of the folder it goes in, empty for the top level.</param>
    /// <param name="contentType">The file's content type.</param>
    /// <param name="content">The file's content.</param>
    /// <param name="ct">A token to cancel with.</param>
    /// <returns>The new file's metadata.</returns>
    Task<VaultFileEntry> AddFileAsync(string name, string? folder, string? contentType, byte[] content, CancellationToken ct = default);

    /// <summary>
    /// Changes a file's name, folder and notes. Its content is not touched.
    /// </summary>
    /// <param name="id">The file's id.</param>
    /// <param name="name">Its name.</param>
    /// <param name="folder">The path of the folder it is in, empty for the top level.</param>
    /// <param name="notes">Its notes, or null for none.</param>
    /// <param name="ct">A token to cancel with.</param>
    /// <returns>The file's metadata, as saved.</returns>
    Task<VaultFileEntry> UpdateFileAsync(string id, string name, string? folder, string? notes, CancellationToken ct = default);

    /// <summary>
    /// Deletes a file from the open vault.
    /// </summary>
    /// <param name="id">The file's id.</param>
    /// <param name="ct">A token to cancel with.</param>
    /// <returns>A task that completes when it is deleted.</returns>
    Task DeleteFileAsync(string id, CancellationToken ct = default);

    /// <summary>
    /// Reads and decrypts a file's content, to show it. The session keeps none of it: each call decrypts again.
    /// </summary>
    /// <param name="id">The file's id.</param>
    /// <param name="ct">A token to cancel with.</param>
    /// <returns>The content, or null if it is not on this device.</returns>
    Task<byte[]?> ReadContentAsync(string id, CancellationToken ct = default);

    /// <summary>
    /// Saves a text file's content: the content is written first, then its metadata, with the new size and time.
    /// </summary>
    /// <param name="id">The file's id.</param>
    /// <param name="content">The new content.</param>
    /// <param name="ct">A token to cancel with.</param>
    /// <returns>The file's metadata, as saved.</returns>
    Task<VaultFileEntry> SaveContentAsync(string id, byte[] content, CancellationToken ct = default);

    /// <summary>
    /// Says the open vault is in use, so it does not lock itself as idle: e.g. while the user is typing in the editor,
    /// which reads and writes nothing.
    /// </summary>
    void KeepUnlocked();

    /// <summary>
    /// Makes an empty folder.
    /// </summary>
    /// <param name="folder">The folder's path.</param>
    /// <param name="ct">A token to cancel with.</param>
    /// <returns>A task that completes when it is made.</returns>
    Task CreateFolderAsync(string folder, CancellationToken ct = default);

    /// <summary>
    /// Renames or moves a folder: the path is rewritten in every file and folder record under it.
    /// </summary>
    /// <param name="from">The folder's path.</param>
    /// <param name="to">Its new path.</param>
    /// <param name="ct">A token to cancel with.</param>
    /// <returns>A task that completes when everything under it has its new path.</returns>
    Task MoveFolderAsync(string from, string to, CancellationToken ct = default);

    /// <summary>
    /// Deletes a folder and everything in it.
    /// </summary>
    /// <param name="folder">The folder's path.</param>
    /// <param name="ct">A token to cancel with.</param>
    /// <returns>A task that completes when it is deleted.</returns>
    Task DeleteFolderAsync(string folder, CancellationToken ct = default);
}
