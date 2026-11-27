using Clypse.Portal.Application.Exceptions;
using Clypse.Portal.Application.Services.Interfaces;
using Clypse.Portal.Models.Files;
using Clypse.Portal.Models.Vaults;
using Devoctomy.Blazwiz.Encryption.Exceptions;
using Devoctomy.Blazwiz.Encryption.Interfaces;
using Devoctomy.Blazwiz.Encryption.Models;
using Devoctomy.Blazwiz.Sync.Encryption.Interfaces;
using Devoctomy.Blazwiz.Sync.Interfaces;
using Devoctomy.Blazwiz.Sync.Models;

namespace Clypse.Portal.Application.Services;

/// <summary>
/// Implementation of <see cref="IVaultFileSession"/>.
/// </summary>
public sealed class VaultFileSession : IVaultFileSession, IDisposable
{
    /// <summary>
    /// How many metadata files are read between telling the page while loading.
    /// </summary>
    public const int ReadBatchSize = 100;

    private readonly IVaultFileServiceFactory fileServiceFactory;
    private readonly IVaultFileTreeService tree;
    private readonly IE2eProtectedProfileManager manager;
    private readonly ISyncService syncService;
    private readonly IE2eIdleLock idleLock;
    private readonly Dictionary<string, VaultFileEntry> entries = new Dictionary<string, VaultFileEntry>(StringComparer.Ordinal);
    private readonly HashSet<string> contentIds = new HashSet<string>(StringComparer.Ordinal);
    private readonly Lock gate = new Lock();
    private int unreadableCount;

    private IVaultFileService? fileService;
    private CancellationTokenSource? loading;
    private IReadOnlyList<VaultFileEntry>? snapshot;

    /// <summary>
    /// Initializes a new instance of the <see cref="VaultFileSession"/> class.
    /// </summary>
    /// <param name="fileServiceFactory">Creates the open vault's file service.</param>
    /// <param name="tree">Works out the folders from the metadata.</param>
    /// <param name="manager">Says when the vault is locked or unlocked.</param>
    /// <param name="syncService">Says when sync changes the vault's files.</param>
    /// <param name="idleLock">Locks the vault when it is idle, and is told when it is in use.</param>
    public VaultFileSession(
        IVaultFileServiceFactory fileServiceFactory,
        IVaultFileTreeService tree,
        IE2eProtectedProfileManager manager,
        ISyncService syncService,
        IE2eIdleLock idleLock)
    {
        this.fileServiceFactory = fileServiceFactory ?? throw new ArgumentNullException(nameof(fileServiceFactory));
        this.tree = tree ?? throw new ArgumentNullException(nameof(tree));
        this.manager = manager ?? throw new ArgumentNullException(nameof(manager));
        this.syncService = syncService ?? throw new ArgumentNullException(nameof(syncService));
        this.idleLock = idleLock ?? throw new ArgumentNullException(nameof(idleLock));
        this.manager.LockStateChanged += OnLockStateChanged;
        this.syncService.FilesChanged += OnFilesChanged;
    }

    /// <inheritdoc/>
    public event EventHandler? Changed;

    /// <inheritdoc/>
    public Vault? Vault { get; private set; }

    /// <inheritdoc/>
    public IReadOnlyList<VaultFileEntry> Entries
    {
        get
        {
            lock (gate)
            {
                return snapshot ??= entries.Values.ToList();
            }
        }
    }

    /// <inheritdoc/>
    public bool IsLoading { get; private set; }

    /// <inheritdoc/>
    public int LoadedCount { get; private set; }

    /// <inheritdoc/>
    public int TotalCount { get; private set; }

    /// <inheritdoc/>
    public int UnreadableCount
    {
        get
        {
            lock (gate)
            {
                return unreadableCount;
            }
        }

        private set
        {
            lock (gate)
            {
                unreadableCount = value;
            }
        }
    }

    /// <inheritdoc/>
    public bool IsUnlocked => Vault != null && manager.IsUnlocked(new E2eKeyScope(Vault.Id));

    /// <inheritdoc/>
    public bool HasContent(string id)
    {
        lock (gate)
        {
            return contentIds.Contains(id);
        }
    }

    /// <inheritdoc/>
    public async Task OpenAsync(Vault vault, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(vault);

        Forget();
        Vault = vault;
        fileService = fileServiceFactory.Create(vault);
        Notify();
        if (IsUnlocked)
        {
            await LoadAsync(ct);
        }
    }

    /// <inheritdoc/>
    public void Close()
    {
        Forget();
        Vault = null;
        fileService = null;
        Notify();
    }

    /// <inheritdoc/>
    public async Task<VaultFileEntry> AddFileAsync(string name, string? folder, string? contentType, byte[] content, CancellationToken ct = default)
    {
        var service = OpenService();
        var entry = await service.AddFileAsync(name, folder, contentType, content, ct);
        Set(entry);
        lock (gate)
        {
            contentIds.Add(entry.Id);
        }

        await RemoveUnneededRecordsAsync(service, entry.Folder, ct);
        Notify();
        return entry;
    }

    /// <inheritdoc/>
    public async Task<VaultFileEntry> UpdateFileAsync(string id, string name, string? folder, string? notes, CancellationToken ct = default)
    {
        var service = OpenService();
        var existing = Find(id);
        if (existing == null || existing.Kind != VaultFileKind.File)
        {
            throw new ClypseException(ClypseErrorCodes.FileNotFound);
        }

        var changed = Copy(existing);
        changed.Name = name;
        changed.Folder = tree.Normalize(folder);
        changed.Notes = notes;
        var saved = await service.UpdateAsync(changed, ct);
        Set(saved);
        if (!string.Equals(saved.Folder, existing.Folder, StringComparison.Ordinal))
        {
            await RemoveUnneededRecordsAsync(service, saved.Folder, ct);
            await KeepIfEmptyAsync(service, existing.Folder, ct);
        }

        Notify();
        return saved;
    }

    /// <inheritdoc/>
    public async Task DeleteFileAsync(string id, CancellationToken ct = default)
    {
        var service = OpenService();
        var existing = Find(id);
        await service.DeleteAsync(id, VaultFileKind.File, ct);
        Remove(id);
        if (existing != null)
        {
            await KeepIfEmptyAsync(service, existing.Folder, ct);
        }

        Notify();
    }

    /// <inheritdoc/>
    public Task<byte[]?> ReadContentAsync(string id, CancellationToken ct = default)
    {
        return OpenService().ReadContentAsync(id, ct);
    }

    /// <inheritdoc/>
    public async Task<VaultFileEntry> SaveContentAsync(string id, byte[] content, CancellationToken ct = default)
    {
        var saved = await OpenService().SaveContentAsync(id, content, ct);
        Set(saved);
        lock (gate)
        {
            contentIds.Add(saved.Id);
        }

        Notify();
        return saved;
    }

    /// <inheritdoc/>
    public void KeepUnlocked()
    {
        if (Vault != null)
        {
            idleLock.Touch(new E2eKeyScope(Vault.Id));
        }
    }

    /// <inheritdoc/>
    public async Task CreateFolderAsync(string folder, CancellationToken ct = default)
    {
        var service = OpenService();
        var path = tree.Normalize(folder);
        if (path.Length == 0)
        {
            throw new ClypseException(ClypseErrorCodes.FolderNameInvalid);
        }

        if (tree.Exists(Entries, path))
        {
            throw new ClypseException(ClypseErrorCodes.FolderExists);
        }

        Set(await service.AddFolderAsync(path, ct));

        // The folder it is in is not empty any more.
        await RemoveUnneededRecordsAsync(service, tree.GetParent(path), ct);
        Notify();
    }

    /// <inheritdoc/>
    public async Task MoveFolderAsync(string from, string to, CancellationToken ct = default)
    {
        var service = OpenService();
        var source = tree.Normalize(from);
        var target = tree.Normalize(to);
        if (source.Length == 0 || target.Length == 0)
        {
            throw new ClypseException(ClypseErrorCodes.FolderNameInvalid);
        }

        if (string.Equals(source, target, StringComparison.Ordinal))
        {
            return;
        }

        if (tree.IsAtOrUnder(target, source))
        {
            throw new ClypseException(ClypseErrorCodes.FolderMoveInvalid);
        }

        if (tree.Exists(Entries, target))
        {
            throw new ClypseException(ClypseErrorCodes.FolderExists);
        }

        var moving = tree.GetEntriesUnder(Entries, source);
        if (moving.Count == 0)
        {
            // Gone meanwhile (e.g. deleted on another device): there is nothing to move.
            return;
        }

        try
        {
            foreach (var entry in moving)
            {
                var changed = Copy(entry);
                changed.Folder = tree.Rebase(entry.Folder, source, target);
                Set(await service.UpdateAsync(changed, ct));
            }

            await RemoveUnneededRecordsAsync(service, tree.GetParent(target), ct);
            await KeepIfEmptyAsync(service, tree.GetParent(source), ct);
        }
        finally
        {
            // Cut short, both folders show, each with some of the files: the page must show that too.
            Notify();
        }
    }

    /// <inheritdoc/>
    public async Task DeleteFolderAsync(string folder, CancellationToken ct = default)
    {
        var service = OpenService();
        var path = tree.Normalize(folder);
        if (path.Length == 0)
        {
            throw new ClypseException(ClypseErrorCodes.FolderNameInvalid);
        }

        var deleting = tree.GetEntriesUnder(Entries, path);
        if (deleting.Count == 0)
        {
            // Gone meanwhile (e.g. deleted on another device): there is nothing to delete.
            return;
        }

        try
        {
            foreach (var entry in deleting)
            {
                await service.DeleteAsync(entry.Id, entry.Kind, ct);
                Remove(entry.Id);
            }

            await KeepIfEmptyAsync(service, tree.GetParent(path), ct);
        }
        finally
        {
            Notify();
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        manager.LockStateChanged -= OnLockStateChanged;
        syncService.FilesChanged -= OnFilesChanged;
        Forget();
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

    private IVaultFileService OpenService()
    {
        return IsUnlocked && fileService != null
            ? fileService
            : throw new ClypseException(ClypseErrorCodes.NoVaultOpen);
    }

    // A folder with something in it needs no record of its own, and nor does any folder above it: what is in it names
    // it. Leftover records do no harm, so this only tidies the folders a change has just put something into.
    private async Task RemoveUnneededRecordsAsync(IVaultFileService service, string folder, CancellationToken ct)
    {
        for (var path = tree.Normalize(folder); path.Length > 0; path = tree.GetParent(path))
        {
            var current = Entries;
            var records = current.Where(e => IsRecordOf(e, path)).ToList();
            if (records.Count == 0 || !current.Any(e => !IsRecordOf(e, path) && tree.IsAtOrUnder(e.Folder, path)))
            {
                continue;
            }

            foreach (var record in records)
            {
                await service.DeleteAsync(record.Id, VaultFileKind.Folder, ct);
                Remove(record.Id);
            }
        }
    }

    // Nothing names a folder once the last thing in it is deleted or moved out: a record keeps it.
    private async Task KeepIfEmptyAsync(IVaultFileService service, string folder, CancellationToken ct)
    {
        var path = tree.Normalize(folder);
        if (path.Length > 0 && !tree.Exists(Entries, path))
        {
            Set(await service.AddFolderAsync(path, ct));
        }
    }

    private bool IsRecordOf(VaultFileEntry entry, string path)
    {
        return entry.Kind == VaultFileKind.Folder && string.Equals(tree.Normalize(entry.Folder), path, StringComparison.Ordinal);
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        var service = fileService!;
        loading?.Cancel();
        var source = CancellationTokenSource.CreateLinkedTokenSource(ct);
        loading = source;
        var token = source.Token;

        lock (gate)
        {
            entries.Clear();
            contentIds.Clear();
            snapshot = null;
        }

        LoadedCount = 0;
        TotalCount = 0;
        UnreadableCount = 0;
        IsLoading = true;
        try
        {
            var listing = await service.ListAsync(token);
            SetContentIds(listing.ContentIds);
            TotalCount = listing.EntryIds.Count;
            Notify();
            foreach (var batch in listing.EntryIds.Chunk(ReadBatchSize))
            {
                token.ThrowIfCancellationRequested();
                await Task.WhenAll(batch.Select(id => ReadAsync(service, id, token)));
                LoadedCount += batch.Length;
                Notify();
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // Locked, closed or another vault opened: whatever replaced this load has reset the state.
            return;
        }
        catch (E2eLockedException)
        {
            // Locked part way through: the lock clears what was read.
            return;
        }
        finally
        {
            if (loading == source)
            {
                loading = null;
                IsLoading = false;
            }

            source.Dispose();
        }

        Notify();
    }

    private async Task ReadAsync(IVaultFileService service, string id, CancellationToken ct)
    {
        try
        {
            var entry = await service.GetAsync(id, ct);

            // Closed, locked or another vault opened while it was read: keep nothing of it.
            ct.ThrowIfCancellationRequested();
            if (entry == null)
            {
                Remove(id);
            }
            else
            {
                Set(entry);
            }
        }
        catch (ClypseException ex) when (ex.Code == ClypseErrorCodes.FileUnreadable)
        {
            Remove(id);
            lock (gate)
            {
                unreadableCount++;
            }
        }
    }

    private VaultFileEntry? Find(string id)
    {
        lock (gate)
        {
            return entries.GetValueOrDefault(id);
        }
    }

    private void Set(VaultFileEntry entry)
    {
        lock (gate)
        {
            entries[entry.Id] = entry;
            snapshot = null;
        }
    }

    private void Remove(string id)
    {
        lock (gate)
        {
            contentIds.Remove(id);
            if (entries.Remove(id))
            {
                snapshot = null;
            }
        }
    }

    private void SetContentIds(IEnumerable<string> ids)
    {
        lock (gate)
        {
            contentIds.Clear();
            contentIds.UnionWith(ids);
        }
    }

    private void Forget()
    {
        loading?.Cancel();
        loading = null;
        lock (gate)
        {
            entries.Clear();
            contentIds.Clear();
            snapshot = null;
        }

        IsLoading = false;
        LoadedCount = 0;
        TotalCount = 0;
        UnreadableCount = 0;
    }

    private void Notify() => Changed?.Invoke(this, EventArgs.Empty);

    private async void OnLockStateChanged(object? sender, E2eKeyScope scope)
    {
        if (Vault == null || scope.Id != Vault.Id)
        {
            return;
        }

        try
        {
            if (IsUnlocked)
            {
                await LoadAsync(CancellationToken.None);
            }
            else
            {
                Forget();
                Notify();
            }
        }
        catch (Exception)
        {
            // Event handlers must not throw. The metadata is read again when the vault is next opened.
        }
    }

    private async void OnFilesChanged(object? sender, SyncFilesChangedEventArgs e)
    {
        var service = fileService;
        if (Vault == null || service == null || e.Profile != Vault.Id || !IsUnlocked || IsLoading)
        {
            // A load in progress reads the changed files anyway.
            return;
        }

        try
        {
            var changed = false;
            if (e.Keys.Any(k => service.GetContentId(k) != null))
            {
                // Content arrived or went: which is here now is found from the listing, without decrypting anything.
                SetContentIds((await service.ListAsync()).ContentIds);
                changed = true;
            }

            foreach (var id in e.Keys.Select(service.GetEntryId).OfType<string>().Distinct())
            {
                await ReadAsync(service, id, CancellationToken.None);
                changed = true;
            }

            if (changed)
            {
                Notify();
            }
        }
        catch (Exception)
        {
            // Event handlers must not throw. Locked meanwhile, or the file went away again: read on the next change.
        }
    }
}
