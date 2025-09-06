namespace clypse.core.Vault;

/// <summary>
/// Defines interface for the creation of vault specific vault managers, dynamically initialized based on the vault being accessed.
/// </summary>
public interface IVaultManagerBootstrapperService
{
    /// <summary>
    /// Creates a vault manager which is suitable for use with a specific vault identified by its Id.
    /// </summary>
    /// <param name="id">The unique identifier of the vault.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>On success returns an instance of IVaultManager suitable for use on the specified vault, otherwise null.</returns>
    public Task<IVaultManager?> CreateVaultManagerForVaultAsync(
        string id,
        CancellationToken cancellationToken);

    /// <summary>
    /// Fetches a list of all vault Ids available in storage.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>List of vault ids found in storage.</returns>
    public Task<List<string>> ListVaultIdsAsync(CancellationToken cancellationToken);
}
