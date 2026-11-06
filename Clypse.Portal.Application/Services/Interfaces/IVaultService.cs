using Clypse.Portal.Models.Vaults;
using Devoctomy.Blazwiz.Encryption.Models;

namespace Clypse.Portal.Application.Services.Interfaces;

/// <summary>
/// The user's vaults. Each is a protected sync profile with its own passphrase and keys; its name and description are
/// kept, encrypted, in a <c>.vault</c> file at its root.
/// </summary>
public interface IVaultService
{
    /// <summary>
    /// Gets the user's vaults. An unlocked vault's name is read from it and kept as its label on this device, so it is
    /// shown by name while locked too.
    /// </summary>
    /// <param name="ct">A token to cancel with.</param>
    /// <returns>The vaults.</returns>
    Task<IReadOnlyList<Vault>> GetVaultsAsync(CancellationToken ct = default);

    /// <summary>
    /// Finds a vault by its id.
    /// </summary>
    /// <param name="id">The vault's id.</param>
    /// <param name="ct">A token to cancel with.</param>
    /// <returns>The vault, or null if there is none with that id.</returns>
    Task<Vault?> FindAsync(string id, CancellationToken ct = default);

    /// <summary>
    /// Creates a vault: a new protected profile with its own passphrase, holding the vault's name and description. It is
    /// unlocked once created.
    /// </summary>
    /// <param name="name">The vault's name.</param>
    /// <param name="description">The vault's description, if any.</param>
    /// <param name="passphrase">The vault's passphrase.</param>
    /// <param name="progress">Reports the key derivation's progress.</param>
    /// <param name="ct">A token to cancel with.</param>
    /// <returns>The vault.</returns>
    Task<Vault> CreateAsync(string name, string? description, string passphrase, IProgress<E2eProgress>? progress = null, CancellationToken ct = default);

    /// <summary>
    /// Changes an unlocked vault's name and description.
    /// </summary>
    /// <param name="id">The vault's id.</param>
    /// <param name="name">The new name.</param>
    /// <param name="description">The new description, if any.</param>
    /// <param name="ct">A token to cancel with.</param>
    /// <returns>The vault.</returns>
    Task<Vault> UpdateAsync(string id, string name, string? description, CancellationToken ct = default);

    /// <summary>
    /// Deletes a vault: its secrets everywhere, and its keys.
    /// </summary>
    /// <param name="id">The vault's id.</param>
    /// <param name="ct">A token to cancel with.</param>
    /// <returns>A task that completes when it is deleted.</returns>
    Task DeleteAsync(string id, CancellationToken ct = default);

    /// <summary>
    /// Locks a vault: its keys are wiped from memory.
    /// </summary>
    /// <param name="id">The vault's id.</param>
    void Lock(string id);
}
