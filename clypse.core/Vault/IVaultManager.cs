namespace clypse.core.Vault
{
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
    }
}
