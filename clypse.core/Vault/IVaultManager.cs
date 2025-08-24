using clypse.core.Secrets;

namespace clypse.core.Vault;

public interface IVaultManager
{
    public IVault Create(
        string name,
        string description);
    public Task SaveAsync(
        IVault vault,
        string base64Key,
        CancellationToken cancellationToken);
    public Task DeleteAsync(
        IVault vault,
        string base64Key,
        CancellationToken cancellationToken);
    public Task<Vault> LoadAsync(
        string id,
        string base64Key,
        CancellationToken cancellationToken);
    public Task<Secret> GetSecretAsync(
            IVault vault,
            string secretId,
            string base64Key,
            CancellationToken cancellationToken);
}
