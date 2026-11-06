using Clypse.Core.Secrets;

namespace Clypse.Portal.Application.Services.Interfaces;

/// <summary>
/// One vault's secrets: each is a file, <c>secrets/{id}.json</c> under the vault's root, holding the secret's data in
/// clypse's format. Files are named only by the secret's id, so nothing about a secret shows in its path. Created for a
/// vault by <see cref="ISecretServiceFactory"/>.
/// </summary>
public interface ISecretService
{
    /// <summary>
    /// Lists the ids of the vault's secrets, without reading them.
    /// </summary>
    /// <param name="ct">A token to cancel with.</param>
    /// <returns>The ids.</returns>
    Task<IReadOnlyList<string>> ListIdsAsync(CancellationToken ct = default);

    /// <summary>
    /// Reads a secret, as its own type (e.g. <see cref="WebSecret"/>).
    /// </summary>
    /// <param name="id">The secret's id.</param>
    /// <param name="ct">A token to cancel with.</param>
    /// <returns>The secret, or null if there is none with that id.</returns>
    Task<Secret?> GetAsync(string id, CancellationToken ct = default);

    /// <summary>
    /// Saves a secret, new or changed, after checking it has everything it needs. Its last updated time is set to now.
    /// </summary>
    /// <param name="secret">The secret.</param>
    /// <param name="ct">A token to cancel with.</param>
    /// <returns>The saved secret.</returns>
    Task<Secret> SaveAsync(Secret secret, CancellationToken ct = default);

    /// <summary>
    /// Deletes a secret.
    /// </summary>
    /// <param name="id">The secret's id.</param>
    /// <param name="ct">A token to cancel with.</param>
    /// <returns>A task that completes when it is deleted.</returns>
    Task DeleteAsync(string id, CancellationToken ct = default);

    /// <summary>
    /// Imports secrets: all of them are checked first, and none is written unless all are valid.
    /// </summary>
    /// <param name="secrets">The secrets.</param>
    /// <param name="progress">Reports how many have been written.</param>
    /// <param name="ct">A token to cancel with.</param>
    /// <returns>How many were imported.</returns>
    Task<int> ImportAsync(IReadOnlyList<Secret> secrets, IProgress<int>? progress = null, CancellationToken ct = default);

    /// <summary>
    /// Gets the id of the secret a file key names, if it is one of this vault's secrets.
    /// </summary>
    /// <param name="key">A file key.</param>
    /// <returns>The secret's id, or null if the key is not a secret of this vault.</returns>
    string? GetSecretId(string key);
}
