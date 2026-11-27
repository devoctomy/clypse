using Clypse.Portal.Application.Exceptions;
using Clypse.Portal.Application.Services.Interfaces;
using Clypse.Portal.Models.Files;

namespace Clypse.Portal.Application.Services;

/// <summary>
/// Implementation of <see cref="IVaultFileTreeService"/>. Paths are compared exactly (ordinal), and shown in order
/// without regard to case.
/// </summary>
public class VaultFileTreeService : IVaultFileTreeService
{
    /// <summary>
    /// What joins the names in a folder's path.
    /// </summary>
    public const char Separator = '/';

    /// <inheritdoc/>
    public string Normalize(string? path)
    {
        return string.IsNullOrWhiteSpace(path)
            ? string.Empty
            : string.Join(Separator, path.Split(Separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    /// <inheritdoc/>
    public bool IsValidName(string? name) => !string.IsNullOrWhiteSpace(name) && !name.Contains(Separator);

    /// <inheritdoc/>
    public string Combine(string? parent, string name)
    {
        if (!IsValidName(name))
        {
            throw new ClypseException(ClypseErrorCodes.FolderNameInvalid);
        }

        return Join(Normalize(parent), name.Trim());
    }

    /// <inheritdoc/>
    public string GetParent(string? path)
    {
        var folder = Normalize(path);
        var cut = folder.LastIndexOf(Separator);
        return cut < 0 ? string.Empty : folder[..cut];
    }

    /// <inheritdoc/>
    public string GetName(string? path)
    {
        var folder = Normalize(path);
        var start = folder.LastIndexOf(Separator) + 1;
        return folder[start..];
    }

    /// <inheritdoc/>
    public bool IsAtOrUnder(string? path, string? folder) => IsAtOrUnderNormalized(Normalize(path), Normalize(folder));

    /// <inheritdoc/>
    public string Rebase(string? path, string from, string to)
    {
        var inner = Normalize(path);
        var source = Normalize(from);
        if (!IsAtOrUnderNormalized(inner, source))
        {
            return inner;
        }

        var rest = source.Length == 0 ? inner : inner[source.Length..].TrimStart(Separator);
        return Join(Normalize(to), rest);
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> GetFolders(IEnumerable<VaultFileEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var folders = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in entries)
        {
            // A folder that is named brings every folder above it with it.
            var path = Normalize(entry.Folder);
            while (path.Length > 0 && folders.Add(path))
            {
                path = GetParent(path);
            }
        }

        return folders
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ThenBy(f => f, StringComparer.Ordinal)
            .ToList();
    }

    /// <inheritdoc/>
    public bool Exists(IEnumerable<VaultFileEntry> entries, string? folder)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var path = Normalize(folder);
        return path.Length == 0 || entries.Any(e => IsAtOrUnderNormalized(Normalize(e.Folder), path));
    }

    /// <inheritdoc/>
    public IReadOnlyList<VaultFileEntry> GetEntriesUnder(IEnumerable<VaultFileEntry> entries, string? folder)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var path = Normalize(folder);
        return entries.Where(e => IsAtOrUnderNormalized(Normalize(e.Folder), path)).ToList();
    }

    /// <inheritdoc/>
    public VaultFolderView GetView(IEnumerable<VaultFileEntry> entries, string? folder)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var path = Normalize(folder);
        var names = new HashSet<string>(StringComparer.Ordinal);
        var files = new List<VaultFileEntry>();
        foreach (var entry in entries)
        {
            var entryFolder = Normalize(entry.Folder);
            if (entryFolder == path)
            {
                if (entry.Kind == VaultFileKind.File)
                {
                    files.Add(entry);
                }
            }
            else if (IsAtOrUnderNormalized(entryFolder, path))
            {
                // Somewhere under this folder: the next name in its path is a folder in this one.
                var start = path.Length == 0 ? 0 : path.Length + 1;
                var rest = entryFolder[start..];
                var end = rest.IndexOf(Separator);
                names.Add(end < 0 ? rest : rest[..end]);
            }
        }

        return new VaultFolderView
        {
            Path = path,
            Breadcrumb = GetBreadcrumb(path),
            Folders = names
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ThenBy(n => n, StringComparer.Ordinal)
                .Select(n => new VaultFolder { Name = n, Path = Join(path, n) })
                .ToList(),
            Files = files
                .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(f => f.Id, StringComparer.Ordinal)
                .ToList(),
        };
    }

    private static string Join(string parent, string rest)
    {
        if (parent.Length == 0)
        {
            return rest;
        }

        return rest.Length == 0 ? parent : parent + Separator + rest;
    }

    private static bool IsAtOrUnderNormalized(string path, string folder)
    {
        return folder.Length == 0
            || string.Equals(path, folder, StringComparison.Ordinal)
            || (path.Length > folder.Length && path[folder.Length] == Separator && path.StartsWith(folder, StringComparison.Ordinal));
    }

    private static List<VaultFolder> GetBreadcrumb(string path)
    {
        var breadcrumb = new List<VaultFolder>();
        var current = string.Empty;
        foreach (var name in path.Split(Separator, StringSplitOptions.RemoveEmptyEntries))
        {
            current = Join(current, name);
            breadcrumb.Add(new VaultFolder { Name = name, Path = current });
        }

        return breadcrumb;
    }
}
