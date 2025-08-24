using clypse.core.Cloud.Exceptions;
using clypse.core.Cloud.Interfaces;
using clypse.core.Compression.Interfaces;
using clypse.core.Secrets;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace clypse.core.Vault;

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

    public IVault Create(
        string name,
        string description)
    {
        var vaultInfo = new VaultInfo(name, description);
        return new Vault(
            vaultInfo,
            new VaultIndex());
    }

    public async Task SaveAsync(
        IVault vault,
        string base64Key,
        CancellationToken cancellationToken)
    {
        if (!vault.IsDirty)
        {
            return;
        }

        await SaveInfoAsync(
            vault.Info,
            base64Key,
            cancellationToken);

        foreach (var secret in vault.PendingSecrets)
        {
            var existing = vault.Index.Entries.SingleOrDefault(x => x.Id == secret.Id);
            if (existing != null)
            {
                vault.Index.Entries.Remove(existing);
            }

            vault.Index.Entries.Add(new VaultIndexEntry
            (
                secret.Id,
                secret.Name!,
                secret.Description,
                string.Join(',', secret.Tags)
            ));
            await SaveObjectAsync(
                secret,
                vault.Info.Id,
                $"secrets/{secret.Id}",
                base64Key,
                cancellationToken);
        }

        foreach (var secret in vault.SecretsToDelete)
        {
            var existing = vault.Index.Entries.SingleOrDefault(x => x.Id == secret);
            if(existing == null)
            {
                continue;
            }

            vault.Index.Entries.Remove(existing);
            await DeleteSecretAsync(
                vault.Info.Id,
                secret,
                base64Key,
                cancellationToken);
        }

        vault.MakeClean();
        await SaveIndexAsync(
            vault.Info,
            vault.Index,
            base64Key,
            cancellationToken);
    }

    public async Task<Vault> LoadAsync(
        string id,
        string base64Key,
        CancellationToken cancellationToken)
    {
        var info = await LoadInfoAsync(
            id,
            base64Key,
            cancellationToken);

        var index = await LoadIndexAsync(
            id,
            base64Key,
            cancellationToken);

        var vault = new Vault(
            info,
            index);
        vault.MakeClean();
        return vault;
    }

    public async Task DeleteAsync(
        IVault vault,
        string base64Key,
        CancellationToken cancellationToken)
    {
        var allKeys = await encryptedCloudStorageProvider.ListObjectsAsync(
            $"{vault.Info.Id}/",
            cancellationToken);
        foreach (var key in allKeys)
        {
            var deleted = await encryptedCloudStorageProvider.DeleteEncryptedObjectAsync(
                key,
                base64Key,
                cancellationToken);
            if (!deleted)
            {
                throw new CloudStorageProviderException($"Failed to delete '{key}' from S3, while deleting vault '{vault.Info.Name}'.");
            }
        }
    }

    public async Task<Secret> GetSecretAsync(
        IVault vault,
        string secretId,
        string base64Key,
        CancellationToken cancellationToken)
    {
        var secret = await LoadObjectAsync<Secret>(
            vault.Info.Id,
            $"secrets/{secretId}",
            base64Key,
            cancellationToken);
        switch(secret.SecretType)
        {
            case Enums.SecretType.None:
                {
                    // Do nothing;
                    break;
                }

            case Enums.SecretType.Web:
                {
                    secret = WebSecret.FromSecret(secret);
                    break;
                }
        }

        return secret;
    }

    private async Task SaveIndexAsync(
        VaultInfo vaultInfo,
        VaultIndex vaultIndex,
        string base64Key,
        CancellationToken cancellationToken)
    {
        await SaveObjectAsync(
            vaultIndex,
            vaultInfo.Id,
            "index.json",
            base64Key,
            cancellationToken);
    }

    private async Task SaveInfoAsync(
        VaultInfo vaultInfo,
        string base64Key,
        CancellationToken cancellationToken)
    {
        await SaveObjectAsync(
            vaultInfo,
            vaultInfo.Id,
            "info.json",
            base64Key,
            cancellationToken);
    }

    private async Task<VaultInfo> LoadInfoAsync(
        string id,
        string base64Key,
        CancellationToken cancellationToken)
    {
        var info = await LoadObjectAsync<VaultInfo>(
            id,
            "info.json",
            base64Key,
            cancellationToken);
        return info!;
    }

    private async Task<VaultIndex> LoadIndexAsync(
        string id,
        string base64Key,
        CancellationToken cancellationToken)
    {
        var index = await LoadObjectAsync<VaultIndex>(
            id,
            "index.json",
            base64Key,
            cancellationToken);
        return index!;
    }

    private async Task SaveObjectAsync(
        object obj,
        string vaultId,
        string key,
        string base64Key,
        CancellationToken cancellationToken)
    {
        var objectKey = $"{vaultId}/{key}";
        var objectStream = new MemoryStream();
        await JsonSerializer.SerializeAsync(
            objectStream,
            obj,
            JsonSerializerOptions,
            cancellationToken);
        objectStream.Seek(0, SeekOrigin.Begin);

        var compressedObject = new MemoryStream();
        await compressionService.CompressAsync(
            objectStream,
            compressedObject,
            cancellationToken);
        compressedObject.Seek(0, SeekOrigin.Begin);

        await encryptedCloudStorageProvider.PutEncryptedObjectAsync(
            objectKey,
            compressedObject,
            base64Key,
            cancellationToken);
    }

    private async Task<T> LoadObjectAsync<T>(
        string vaultId,
        string key,
        string base64Key,
        CancellationToken cancellationToken)
    {
        var objectKey = $"{vaultId}/{key}";
        var encryptedCompressedStream = await encryptedCloudStorageProvider.GetEncryptedObjectAsync(
            objectKey,
            base64Key,
            cancellationToken);

        var decompressedStream = new MemoryStream();
        await compressionService.DecompressAsync(
            encryptedCompressedStream!,
            decompressedStream,
            cancellationToken);
        decompressedStream.Seek(0, SeekOrigin.Begin);

        var value = await JsonSerializer.DeserializeAsync<T>(
            decompressedStream,
            JsonSerializerOptions,
            cancellationToken);

        return value!;
    }

    private async Task<bool> DeleteSecretAsync(
        string vaultId,
        string secretId,
        string base64Key,
        CancellationToken cancellationToken)
    {
        var objectKey = $"{vaultId}/secrets/{secretId}";
        var deleted = await encryptedCloudStorageProvider.DeleteEncryptedObjectAsync(
            objectKey,
            base64Key,
            cancellationToken);
        return deleted!;
    }
}
