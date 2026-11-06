using Clypse.Core.Secrets;
using Clypse.Portal.Application.Exceptions;
using Clypse.Portal.Application.Services.Interfaces;
using Clypse.Portal.Models.Vaults;
using Devoctomy.Blazwiz.Encryption.Exceptions;
using Devoctomy.Blazwiz.Encryption.Interfaces;
using Devoctomy.Blazwiz.Encryption.Models;
using Devoctomy.Blazwiz.Sync.Interfaces;
using Devoctomy.Blazwiz.Sync.Models;

namespace Clypse.Portal.Application.Services;

/// <summary>
/// Implementation of <see cref="IVaultSession"/>.
/// </summary>
public sealed class VaultSession : IVaultSession, IDisposable
{
    /// <summary>
    /// How many secrets are read between notifications while loading, so a large vault does not re-render the page for
    /// every secret.
    /// </summary>
    public const int NotifyEvery = 25;

    private readonly ISecretServiceFactory secretServiceFactory;
    private readonly IE2eProtectedProfileManager manager;
    private readonly ISyncService syncService;
    private readonly Dictionary<string, Secret> secrets = [];

    private ISecretService? secretService;
    private CancellationTokenSource? loading;
    private IReadOnlyList<Secret>? snapshot;

    /// <summary>
    /// Initializes a new instance of the <see cref="VaultSession"/> class.
    /// </summary>
    /// <param name="secretServiceFactory">Creates the open vault's secret service.</param>
    /// <param name="manager">Says when the vault is locked or unlocked.</param>
    /// <param name="syncService">Says when sync changes the vault's files.</param>
    public VaultSession(ISecretServiceFactory secretServiceFactory, IE2eProtectedProfileManager manager, ISyncService syncService)
    {
        this.secretServiceFactory = secretServiceFactory ?? throw new ArgumentNullException(nameof(secretServiceFactory));
        this.manager = manager ?? throw new ArgumentNullException(nameof(manager));
        this.syncService = syncService ?? throw new ArgumentNullException(nameof(syncService));
        this.manager.LockStateChanged += OnLockStateChanged;
        this.syncService.FilesChanged += OnFilesChanged;
    }

    /// <inheritdoc/>
    public event EventHandler? Changed;

    /// <inheritdoc/>
    public Vault? Vault { get; private set; }

    /// <inheritdoc/>
    public IReadOnlyList<Secret> Secrets => snapshot ??= secrets.Values.ToList();

    /// <inheritdoc/>
    public bool IsLoading { get; private set; }

    /// <inheritdoc/>
    public int LoadedCount { get; private set; }

    /// <inheritdoc/>
    public int TotalCount { get; private set; }

    /// <inheritdoc/>
    public int UnreadableCount { get; private set; }

    private bool IsUnlocked => Vault != null && manager.IsUnlocked(new E2eKeyScope(Vault.Id));

    /// <inheritdoc/>
    public async Task OpenAsync(Vault vault, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(vault);

        Forget();
        Vault = vault;
        secretService = secretServiceFactory.Create(vault);
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
        secretService = null;
        Notify();
    }

    /// <inheritdoc/>
    public async Task<Secret> SaveAsync(Secret secret, CancellationToken ct = default)
    {
        var saved = await OpenService().SaveAsync(secret, ct);
        Set(saved);
        Notify();
        return saved;
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        await OpenService().DeleteAsync(id, ct);
        Remove(id);
        Notify();
    }

    /// <inheritdoc/>
    public async Task<int> ImportAsync(IReadOnlyList<Secret> secrets, IProgress<int>? progress = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(secrets);

        var imported = await OpenService().ImportAsync(secrets, progress, ct);
        foreach (var secret in secrets)
        {
            Set(secret);
        }

        Notify();
        return imported;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        manager.LockStateChanged -= OnLockStateChanged;
        syncService.FilesChanged -= OnFilesChanged;
        Forget();
    }

    private ISecretService OpenService()
    {
        return IsUnlocked && secretService != null
            ? secretService
            : throw new ClypseException(ClypseErrorCodes.NoVaultOpen);
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        var service = secretService!;
        loading?.Cancel();
        var source = CancellationTokenSource.CreateLinkedTokenSource(ct);
        loading = source;
        var token = source.Token;

        secrets.Clear();
        snapshot = null;
        LoadedCount = 0;
        TotalCount = 0;
        UnreadableCount = 0;
        IsLoading = true;
        try
        {
            var ids = await service.ListIdsAsync(token);
            TotalCount = ids.Count;
            Notify();
            foreach (var id in ids)
            {
                token.ThrowIfCancellationRequested();
                await ReadAsync(service, id, token);
                LoadedCount++;
                if (LoadedCount % NotifyEvery == 0)
                {
                    Notify();
                }
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

    private async Task ReadAsync(ISecretService service, string id, CancellationToken ct)
    {
        try
        {
            var secret = await service.GetAsync(id, ct);

            // Closed, locked or another vault opened while it was read: keep nothing of it.
            ct.ThrowIfCancellationRequested();
            if (secret == null)
            {
                Remove(id);
            }
            else
            {
                Set(secret);
            }
        }
        catch (ClypseException ex) when (ex.Code == ClypseErrorCodes.SecretUnreadable)
        {
            Remove(id);
            UnreadableCount++;
        }
    }

    private void Set(Secret secret)
    {
        secrets[secret.Id] = secret;
        snapshot = null;
    }

    private void Remove(string id)
    {
        if (secrets.Remove(id))
        {
            snapshot = null;
        }
    }

    private void Forget()
    {
        loading?.Cancel();
        loading = null;
        secrets.Clear();
        snapshot = null;
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
                Vault.IsUnlocked = true;
                await LoadAsync(CancellationToken.None);
            }
            else
            {
                Vault.IsUnlocked = false;
                Forget();
                Notify();
            }
        }
        catch (Exception)
        {
            // Event handlers must not throw. The secrets are read again when the vault is next opened.
        }
    }

    private async void OnFilesChanged(object? sender, SyncFilesChangedEventArgs e)
    {
        var service = secretService;
        if (Vault == null || service == null || e.Profile != Vault.Id || !IsUnlocked || IsLoading)
        {
            // A load in progress reads the changed files anyway.
            return;
        }

        try
        {
            var changed = false;
            foreach (var id in e.Keys.Select(service.GetSecretId).OfType<string>().Distinct())
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
