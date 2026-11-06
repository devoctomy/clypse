using Clypse.Core.Secrets;
using Clypse.Portal.Models.Vaults;

namespace Clypse.Portal.Application.Services.Interfaces;

/// <summary>
/// The open vault and its secrets, held in memory while it is unlocked. There is no stored index: once the vault is
/// unlocked, every secret is read and decrypted, and the list fills in as each is ready. Locking the vault (by hand, on
/// idle, or in another tab) clears them. Changes synced from another device are applied as they arrive.
/// </summary>
public interface IVaultSession
{
    /// <summary>
    /// Raised when anything the session holds changes: the vault, its secrets, or the progress of loading them.
    /// </summary>
    event EventHandler? Changed;

    /// <summary>
    /// Gets the open vault, or null if none is open.
    /// </summary>
    Vault? Vault { get; }

    /// <summary>
    /// Gets the vault's secrets read so far.
    /// </summary>
    IReadOnlyList<Secret> Secrets { get; }

    /// <summary>
    /// Gets a value indicating whether the secrets are being read.
    /// </summary>
    bool IsLoading { get; }

    /// <summary>
    /// Gets how many of the vault's secret files have been read so far.
    /// </summary>
    int LoadedCount { get; }

    /// <summary>
    /// Gets how many secret files the vault has.
    /// </summary>
    int TotalCount { get; }

    /// <summary>
    /// Gets how many secret files could not be read as secrets.
    /// </summary>
    int UnreadableCount { get; }

    /// <summary>
    /// Opens a vault. If it is unlocked, its secrets are read; otherwise they are read once it is unlocked.
    /// </summary>
    /// <param name="vault">The vault.</param>
    /// <param name="ct">A token to cancel with.</param>
    /// <returns>A task that completes when the secrets have been read (or straight away, if the vault is locked).</returns>
    Task OpenAsync(Vault vault, CancellationToken ct = default);

    /// <summary>
    /// Closes the open vault, forgetting its secrets. The vault is not locked.
    /// </summary>
    void Close();

    /// <summary>
    /// Saves a secret to the open vault.
    /// </summary>
    /// <param name="secret">The secret.</param>
    /// <param name="ct">A token to cancel with.</param>
    /// <returns>The saved secret.</returns>
    Task<Secret> SaveAsync(Secret secret, CancellationToken ct = default);

    /// <summary>
    /// Deletes a secret from the open vault.
    /// </summary>
    /// <param name="id">The secret's id.</param>
    /// <param name="ct">A token to cancel with.</param>
    /// <returns>A task that completes when it is deleted.</returns>
    Task DeleteAsync(string id, CancellationToken ct = default);

    /// <summary>
    /// Imports secrets into the open vault: none is written unless all are valid.
    /// </summary>
    /// <param name="secrets">The secrets.</param>
    /// <param name="progress">Reports how many have been written.</param>
    /// <param name="ct">A token to cancel with.</param>
    /// <returns>How many were imported.</returns>
    Task<int> ImportAsync(IReadOnlyList<Secret> secrets, IProgress<int>? progress = null, CancellationToken ct = default);
}
