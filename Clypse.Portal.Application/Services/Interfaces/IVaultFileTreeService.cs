using Clypse.Portal.Models.Files;

namespace Clypse.Portal.Application.Services.Interfaces;

/// <summary>
/// Works out a vault's folders from its files' metadata. Folders are virtual: a folder exists because a file's metadata
/// names it (or names a folder inside it), or because an empty folder's record does. A folder's path is its names
/// joined by <c>/</c>, and the top level is the empty path.
/// </summary>
public interface IVaultFileTreeService
{
    /// <summary>
    /// Tidies a folder path: each name trimmed, and empty names dropped.
    /// </summary>
    /// <param name="path">The path, or null for the top level.</param>
    /// <returns>The tidied path, empty for the top level.</returns>
    string Normalize(string? path);

    /// <summary>
    /// Checks a folder's name: it must not be empty, and cannot contain <c>/</c>.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns>True if a folder can have that name.</returns>
    bool IsValidName(string? name);

    /// <summary>
    /// Gets the path of a folder inside another.
    /// </summary>
    /// <param name="parent">The path of the folder it is in, empty for the top level.</param>
    /// <param name="name">The folder's name.</param>
    /// <returns>The folder's path.</returns>
    string Combine(string? parent, string name);

    /// <summary>
    /// Gets the path of the folder a folder is in.
    /// </summary>
    /// <param name="path">The folder's path.</param>
    /// <returns>Its parent's path, empty when it is at the top level.</returns>
    string GetParent(string? path);

    /// <summary>
    /// Gets a folder's name: the end of its path.
    /// </summary>
    /// <param name="path">The folder's path.</param>
    /// <returns>Its name, empty for the top level.</returns>
    string GetName(string? path);

    /// <summary>
    /// Checks whether a path is a folder or somewhere inside it.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <param name="folder">The folder's path. Everything is at or under the top level.</param>
    /// <returns>True if the path is the folder, or under it.</returns>
    bool IsAtOrUnder(string? path, string? folder);

    /// <summary>
    /// Moves a path from under one folder to under another, e.g. when the folder is renamed.
    /// </summary>
    /// <param name="path">A path at or under <paramref name="from"/>.</param>
    /// <param name="from">The folder's path as it was.</param>
    /// <param name="to">The folder's new path.</param>
    /// <returns>The path as it is under <paramref name="to"/>.</returns>
    string Rebase(string? path, string from, string to);

    /// <summary>
    /// Gets every folder the entries name: each file's folder, each empty folder's record, and every folder above
    /// them.
    /// </summary>
    /// <param name="entries">The vault's files and folder records.</param>
    /// <returns>The folders' paths, in order.</returns>
    IReadOnlyList<string> GetFolders(IEnumerable<VaultFileEntry> entries);

    /// <summary>
    /// Checks whether a folder exists: something names it, or it is the top level.
    /// </summary>
    /// <param name="entries">The vault's files and folder records.</param>
    /// <param name="folder">The folder's path.</param>
    /// <returns>True if it exists.</returns>
    bool Exists(IEnumerable<VaultFileEntry> entries, string? folder);

    /// <summary>
    /// Gets the files and folder records in a folder and in every folder under it.
    /// </summary>
    /// <param name="entries">The vault's files and folder records.</param>
    /// <param name="folder">The folder's path.</param>
    /// <returns>The entries at or under it.</returns>
    IReadOnlyList<VaultFileEntry> GetEntriesUnder(IEnumerable<VaultFileEntry> entries, string? folder);

    /// <summary>
    /// Gets a folder as the page shows it: its breadcrumb, and the folders and files in it, by name.
    /// </summary>
    /// <param name="entries">The vault's files and folder records.</param>
    /// <param name="folder">The folder's path, empty for the top level.</param>
    /// <returns>The folder.</returns>
    VaultFolderView GetView(IEnumerable<VaultFileEntry> entries, string? folder);
}
