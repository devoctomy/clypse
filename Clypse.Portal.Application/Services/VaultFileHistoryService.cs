using Clypse.Portal.Application.Services.Interfaces;
using Devoctomy.Blazwiz.Sync;
using Devoctomy.Blazwiz.Sync.Interfaces;
using Devoctomy.Blazwiz.Sync.Models;

namespace Clypse.Portal.Application.Services;

/// <summary>
/// clypse's <see cref="ISyncFileHistoryService"/>: blazwiz's (<see cref="SyncFileHistoryService"/>), except that a
/// restored version of the open vault's secrets and files is saved as their editors save them. A secret is saved
/// through <see cref="IVaultSession.SaveAsync"/>, so the vault's list has it and its times are set. A file's content is
/// saved through <see cref="IVaultFileSession.SaveContentAsync"/>, so the file's details get its new size and time too.
/// Restoring anything else is left to blazwiz's.
/// </summary>
public sealed class VaultFileHistoryService : ISyncFileHistoryService
{
    private readonly SyncFileHistoryService history;
    private readonly IVaultSession secrets;
    private readonly IVaultFileSession session;

    /// <summary>
    /// Initializes a new instance of the <see cref="VaultFileHistoryService"/> class.
    /// </summary>
    /// <param name="history">blazwiz's history service, which lists, reads and restores the versions.</param>
    /// <param name="secrets">The open vault's secrets, which saves a restored secret.</param>
    /// <param name="session">The open vault's files, which saves a restored file's content with its details.</param>
    public VaultFileHistoryService(SyncFileHistoryService history, IVaultSession secrets, IVaultFileSession session)
    {
        this.history = history ?? throw new ArgumentNullException(nameof(history));
        this.secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
        this.session = session ?? throw new ArgumentNullException(nameof(session));
    }

    /// <inheritdoc/>
    public bool IsAvailable => history.IsAvailable;

    /// <inheritdoc/>
    public Task<IReadOnlyList<SyncFileVersion>> GetVersionsAsync(string profile, string key, CancellationToken ct = default)
    {
        return history.GetVersionsAsync(profile, key, ct);
    }

    /// <inheritdoc/>
    public Task<SyncFileVersionContent?> ReadVersionAsync(SyncFileVersion version, CancellationToken ct = default)
    {
        return history.ReadVersionAsync(version, ct);
    }

    /// <inheritdoc/>
    public async Task RestoreAsync(SyncFileVersionContent version, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(version);

        var profile = version.Version.Profile;
        var key = version.Version.Key;
        if (secrets.Vault?.Id == profile && secrets.GetSecretId(key) is { } secretId)
        {
            await secrets.SaveAsync(secrets.ReadSecret(secretId, version.Content), ct);
        }
        else if (session.Vault?.Id == profile && session.GetContentId(key) is { } fileId)
        {
            await session.SaveContentAsync(fileId, version.Content, ct);
        }
        else
        {
            await history.RestoreAsync(version, ct);
        }
    }
}
