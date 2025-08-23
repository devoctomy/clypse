using clypse.core.Cloud.Interfaces;
using clypse.core.Compression.Interfaces;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace clypse.core.Vault
{
    public class VaultManager(
        ICompressionService compressionService,
        IEncryptedCloudStorageProvider encryptedCloudStorageProvider) : IVaultManager
    {
        private readonly JsonSerializerOptions JsonSerializerOptions = new()
        {
            WriteIndented = true,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            }
        };

        public Vault Create(
            string name,
            string description)
        {
            var vaultInfo = new VaultInfo(name, description);
            return new Vault(
                vaultInfo,
                new VaultIndex());
        }

        public async Task SaveAsync(
            Vault vault,
            string base64Key,
            CancellationToken cancellationToken)
        {
            await SaveIndex(
                vault.Index,
                base64Key,
                cancellationToken);
        }

        private async Task SaveIndex(
            VaultIndex vaultIndex,
            string base64Key,
            CancellationToken cancellationToken)
        {
            var indexKey = "index.json";
            var indexStream = new MemoryStream();
            await JsonSerializer.SerializeAsync(
                indexStream,
                vaultIndex,
                JsonSerializerOptions,
                cancellationToken);
            indexStream.Seek(0, SeekOrigin.Begin);

            var compressedIndex = new MemoryStream();
            await compressionService.CompressAsync(indexStream, compressedIndex);
            compressedIndex.Seek(0, SeekOrigin.Begin);

            await encryptedCloudStorageProvider.PutEncryptedObjectAsync(
                indexKey,
                compressedIndex,
                base64Key,
                cancellationToken);
        }
    }
}
