using System.Text.Json;
using System.Text.Json.Serialization;
using Clypse.Portal.Application.Exceptions;
using Clypse.Portal.Application.Services.Interfaces;
using Clypse.Portal.Models.Files;
using Clypse.Portal.Models.Settings;
using Clypse.Portal.Models.Vaults;
using Devoctomy.Blazwiz.Sync.Interfaces;

namespace Clypse.Portal.Application.Services;

/// <summary>
/// Implementation of <see cref="IVaultFileService"/> over a vault's file store, which encrypts and decrypts with the
/// vault's own keys. Content is always written before the metadata that describes it, and metadata deleted before its
/// content, so anything cut short leaves content with no metadata: never a listed file that cannot be opened.
/// </summary>
/// <param name="vault">The vault.</param>
/// <param name="fileStore">The vault's file store.</param>
/// <param name="tree">Tidies folder paths.</param>
/// <param name="settings">Clypse's settings, for the largest file a vault takes.</param>
/// <param name="timeProvider">The clock, for when a file was added and last changed.</param>
public class VaultFileService(
    Vault vault,
    ISyncFileStore fileStore,
    IVaultFileTreeService tree,
    ClypseSettings settings,
    TimeProvider timeProvider)
    : IVaultFileService
{
    /// <summary>
    /// The folder, under a vault's root, holding its files' metadata.
    /// </summary>
    public const string MetadataFolder = "files/meta/";

    /// <summary>
    /// The extension of a metadata file.
    /// </summary>
    public const string MetadataExtension = ".json";

    /// <summary>
    /// The folder, under a vault's root, holding its files' content.
    /// </summary>
    public const string ContentFolder = "files/blobs/";

    /// <summary>
    /// The extension of a content file, whatever the file's own type: nothing about a file shows in its path.
    /// </summary>
    public const string ContentExtension = ".bin";

    /// <summary>
    /// The content type of a file whose type is not known.
    /// </summary>
    public const string UnknownContentType = "application/octet-stream";

    private const long BytesPerMegabyte = 1024 * 1024;

    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter<VaultFileKind>() },
    };

    private readonly Vault vault = vault ?? throw new ArgumentNullException(nameof(vault));
    private readonly ISyncFileStore fileStore = fileStore ?? throw new ArgumentNullException(nameof(fileStore));
    private readonly IVaultFileTreeService tree = tree ?? throw new ArgumentNullException(nameof(tree));
    private readonly ClypseSettings settings = settings ?? throw new ArgumentNullException(nameof(settings));
    private readonly TimeProvider timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    private string MetadataRoot => vault.Root + MetadataFolder;

    private string ContentRoot => vault.Root + ContentFolder;

    /// <inheritdoc/>
    public async Task<VaultFileListing> ListAsync(CancellationToken ct = default)
    {
        var files = (await fileStore.ListAsync(ct)).Where(f => !f.IsFolder).ToList();
        return new VaultFileListing
        {
            EntryIds = files.Select(f => GetEntryId(f.Key)).OfType<string>().ToList(),
            ContentIds = files.Select(f => GetContentId(f.Key)).OfType<string>().ToList(),
        };
    }

    /// <inheritdoc/>
    public async Task<VaultFileEntry?> GetAsync(string id, CancellationToken ct = default)
    {
        var file = await fileStore.ReadAsync(MetadataKeyOf(id), ct);
        if (file == null)
        {
            return null;
        }

        VaultFileEntry? entry;
        try
        {
            entry = JsonSerializer.Deserialize<VaultFileEntry>(file.Content, JsonOptions);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            throw new ClypseException(ClypseErrorCodes.FileUnreadable, ex);
        }

        if (entry == null || entry.Id != id || !IsComplete(entry))
        {
            throw new ClypseException(ClypseErrorCodes.FileUnreadable);
        }

        entry.Folder = tree.Normalize(entry.Folder);
        return entry;
    }

    /// <inheritdoc/>
    public async Task<VaultFileEntry> AddFileAsync(string name, string? folder, string? contentType, byte[] content, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        var now = timeProvider.GetUtcNow();
        var entry = new VaultFileEntry
        {
            Id = Guid.NewGuid().ToString("D"),
            Kind = VaultFileKind.File,
            Name = ValidName(name),
            Folder = tree.Normalize(folder),
            ContentType = string.IsNullOrWhiteSpace(contentType) ? UnknownContentType : contentType.Trim(),
            Size = content.LongLength,
            CreatedAt = now,
            LastUpdatedAt = now,
        };
        EnsureNotTooLarge(content);

        await fileStore.WriteAsync(ContentKeyOf(entry.Id), content, now, ct);
        await WriteAsync(entry, now, ct);
        return entry;
    }

    /// <inheritdoc/>
    public async Task<VaultFileEntry> AddFolderAsync(string folder, CancellationToken ct = default)
    {
        var path = tree.Normalize(folder);
        if (path.Length == 0)
        {
            throw new ClypseException(ClypseErrorCodes.FolderNameInvalid);
        }

        var now = timeProvider.GetUtcNow();
        var entry = new VaultFileEntry
        {
            Id = Guid.NewGuid().ToString("D"),
            Kind = VaultFileKind.Folder,
            Folder = path,
            CreatedAt = now,
            LastUpdatedAt = now,
        };
        await WriteAsync(entry, now, ct);
        return entry;
    }

    /// <inheritdoc/>
    public async Task<VaultFileEntry> UpdateAsync(VaultFileEntry entry, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        MetadataKeyOf(entry.Id);
        var now = timeProvider.GetUtcNow();
        var saved = Copy(entry);
        saved.Folder = tree.Normalize(entry.Folder);
        saved.LastUpdatedAt = now;
        if (saved.Kind == VaultFileKind.File)
        {
            saved.Name = ValidName(entry.Name);
            saved.Notes = string.IsNullOrWhiteSpace(entry.Notes) ? null : entry.Notes.Trim();
        }
        else if (saved.Folder.Length == 0)
        {
            throw new ClypseException(ClypseErrorCodes.FolderNameInvalid);
        }

        await WriteAsync(saved, now, ct);
        return saved;
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(string id, VaultFileKind kind = VaultFileKind.File, CancellationToken ct = default)
    {
        await fileStore.DeleteAsync(MetadataKeyOf(id), ct);
        if (kind == VaultFileKind.File)
        {
            // A folder record has no content: deleting what was never there would still leave a marker to sync.
            await fileStore.DeleteAsync(ContentKeyOf(id), ct);
        }
    }

    /// <inheritdoc/>
    public async Task<byte[]?> ReadContentAsync(string id, CancellationToken ct = default)
    {
        return (await fileStore.ReadAsync(ContentKeyOf(id), ct))?.Content;
    }

    /// <inheritdoc/>
    public async Task<VaultFileEntry> SaveContentAsync(string id, byte[] content, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        var entry = await GetAsync(id, ct);
        if (entry == null || entry.Kind != VaultFileKind.File)
        {
            throw new ClypseException(ClypseErrorCodes.FileNotFound);
        }

        EnsureNotTooLarge(content);
        var now = timeProvider.GetUtcNow();
        entry.Size = content.LongLength;
        entry.LastUpdatedAt = now;
        await fileStore.WriteAsync(ContentKeyOf(id), content, now, ct);
        await WriteAsync(entry, now, ct);
        return entry;
    }

    /// <inheritdoc/>
    public string? GetEntryId(string key) => GetId(key, MetadataRoot, MetadataExtension);

    /// <inheritdoc/>
    public string? GetContentId(string key) => GetId(key, ContentRoot, ContentExtension);

    private static bool IsValidId(string? id) => Guid.TryParseExact(id, "D", out _);

    // Only a GUID may name a file: it keeps a file's name out of its path, and a path out of its id.
    private static string ValidId(string id) => IsValidId(id) ? id : throw new ClypseException(ClypseErrorCodes.FileIdInvalid);

    private static string? GetId(string key, string root, string extension)
    {
        if (key == null
            || !key.StartsWith(root, StringComparison.Ordinal)
            || !key.EndsWith(extension, StringComparison.Ordinal)
            || key.Length < root.Length + extension.Length)
        {
            return null;
        }

        var id = key[root.Length..^extension.Length];
        return IsValidId(id) ? id : null;
    }

    private static string ValidName(string? name)
    {
        return string.IsNullOrWhiteSpace(name)
            ? throw new ClypseException(ClypseErrorCodes.FileNameRequired)
            : name.Trim();
    }

    // A file has a name; an empty folder's record has a path.
    private static bool IsComplete(VaultFileEntry entry)
    {
        return entry.Kind == VaultFileKind.File
            ? !string.IsNullOrWhiteSpace(entry.Name)
            : !string.IsNullOrWhiteSpace(entry.Folder);
    }

    private static VaultFileEntry Copy(VaultFileEntry entry)
    {
        return new VaultFileEntry
        {
            Id = entry.Id,
            Kind = entry.Kind,
            Name = entry.Name,
            Folder = entry.Folder,
            ContentType = entry.ContentType,
            Size = entry.Size,
            Notes = entry.Notes,
            CreatedAt = entry.CreatedAt,
            LastUpdatedAt = entry.LastUpdatedAt,
        };
    }

    private void EnsureNotTooLarge(byte[] content)
    {
        if (content.LongLength > settings.MaxFileSizeMegabytes * BytesPerMegabyte)
        {
            throw new ClypseException(ClypseErrorCodes.FileTooLarge, settings.MaxFileSizeMegabytes);
        }
    }

    private Task WriteAsync(VaultFileEntry entry, DateTimeOffset now, CancellationToken ct)
    {
        return fileStore.WriteAsync(MetadataKeyOf(entry.Id), JsonSerializer.SerializeToUtf8Bytes(entry, JsonOptions), now, ct);
    }

    private string MetadataKeyOf(string id) => MetadataRoot + ValidId(id) + MetadataExtension;

    private string ContentKeyOf(string id) => ContentRoot + ValidId(id) + ContentExtension;
}
