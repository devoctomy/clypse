namespace clypse.core.Vault
{
    public interface IVaultManager
    {
        public Vault Create(
            string name,
            string description);
        public Task SaveAsync(
            Vault vault,
            string base64Key,
            CancellationToken cancellationToken);
    }
}
