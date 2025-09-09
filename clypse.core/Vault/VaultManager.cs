using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.S3.Model;
using clypse.core.Cloud.Exceptions;
using clypse.core.Cloud.Interfaces;
using clypse.core.Compression.Interfaces;
using clypse.core.Cryptogtaphy;
using clypse.core.Cryptogtaphy.Interfaces;
using clypse.core.Json;
using clypse.core.Secrets;
using clypse.core.Vault.Exceptions;

namespace clypse.core.Vault;

/// <summary>
/// Manages vault operations including creation, saving, loading, and deletion of vaults and their secrets.
/// </summary>
public class VaultManager : IVaultManager
{
    private readonly JsonSerializerOptions jsonSerializerOptions = new ()
    {
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
            new JElementToPrimativesConverter(),
        },
    };

    private readonly IKeyDerivationService keyDerivationService;
    private readonly ICompressionService compressionService;
    private readonly IEncryptedCloudStorageProvider encryptedCloudStorageProvider;
    private readonly string prefix;
    private bool disposed = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="VaultManager"/> class with the specified parameters.
    /// </summary>
    /// <param name="prefix">Prefix to use for all S3 object keys.</param>
    /// <param name="keyDerivationService">The key derivation service for deriving cryptographic keys.</param>
    /// <param name="compressionService">The compression service for data compression.</param>
    /// <param name="encryptedCloudStorageProvider">The encrypted cloud storage provider for secure data storage.</param>
    public VaultManager(
        string prefix,
        IKeyDerivationService keyDerivationService,
        ICompressionService compressionService,
        IEncryptedCloudStorageProvider encryptedCloudStorageProvider)
    {
        this.prefix = prefix;
        this.keyDerivationService = keyDerivationService;
        this.compressionService = compressionService;
        this.encryptedCloudStorageProvider = encryptedCloudStorageProvider;
    }

    /// <summary>
    /// Gets the key derivation service used by this vault manager.
    /// </summary>
    public IKeyDerivationService KeyDerivationService => this.keyDerivationService;

    /// <summary>
    /// Gets the compression service used by this vault manager.
    /// </summary>
    public ICompressionService CompressionService => this.compressionService;

    /// <summary>
    /// Gets the encrypted cloud storage provider used by this vault manager.
    /// </summary>
    public IEncryptedCloudStorageProvider EncryptedCloudStorageProvider => this.encryptedCloudStorageProvider;

    /// <summary>
    /// Gets the prefix used for all S3 object keys.
    /// </summary>
    public string Prefix => this.prefix;

    /// <summary>
    /// Derives a cryptographic key from the provided passphrase for the specified vault.
    /// </summary>
    /// <param name="vaultId">The unique identifier of the vault.</param>
    /// <param name="passphrase">The passphrase to derive the key from.</param>
    /// <returns>A byte array containing the derived cryptographic key.</returns>
    public async Task<byte[]> DeriveKeyFromPassphraseAsync(
        string vaultId,
        string passphrase)
    {
        var salt = CryptoHelpers.Sha256HashString(vaultId, 16);
        var base64Salt = Convert.ToBase64String(salt);
        return await this.keyDerivationService.DeriveKeyFromPassphraseAsync(
            passphrase,
            base64Salt);
    }

    /// <summary>
    /// Creates a new vault with the specified name and description.
    /// </summary>
    /// <param name="name">The name of the vault.</param>
    /// <param name="description">The description of the vault.</param>
    /// <returns>A new vault instance.</returns>
    public IVault Create(
        string name,
        string description)
    {
        var parameters = new Dictionary<string, string>();
        var manifest = this.CreateManifest();
        var vaultInfo = new VaultInfo(name, description);
        return new Vault(
            manifest,
            vaultInfo,
            new VaultIndex());
    }

    /// <summary>
    /// Fetches a list of all vault Ids available in storage.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>List of vault ids found in storage.</returns>
    public async Task<List<string>> ListVaultIdsAsync(CancellationToken cancellationToken)
    {
        var allObjectsPrefix = $"{this.prefix}/";
        var allObjects = await this.encryptedCloudStorageProvider.ListObjectsAsync(
            allObjectsPrefix,
            "/",
            cancellationToken);
        return allObjects;
    }

    /// <summary>
    /// Saves the vault and all pending changes to encrypted cloud storage.
    /// </summary>
    /// <param name="vault">The vault to save.</param>
    /// <param name="base64Key">The base64-encoded encryption key.</param>
    /// <param name="metaData">Optional metadata to associate with the vault.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The results of the save operation.</returns>
    public async Task<VaultSaveResults> SaveAsync(
        IVault vault,
        string base64Key,
        Dictionary<string, string>? metaData,
        CancellationToken cancellationToken)
    {
        var results = new VaultSaveResults();
        if (!vault.IsDirty)
        {
            return results;
        }

        await this.SaveManifestAsync(
            vault.Info,
            vault.Manifest,
            cancellationToken);

        await this.SaveInfoAsync(
            vault.Info,
            base64Key,
            metaData,
            cancellationToken);

        foreach (var secret in vault.PendingSecrets)
        {
            var existing = vault.Index.Entries.SingleOrDefault(x => x.Id == secret.Id);
            var updating = false;
            if (existing != null)
            {
                vault.Index.Entries.Remove(existing);
                updating = true;
            }

            secret.LastUpdatedAt = DateTime.UtcNow;
            vault.Index.Entries.Add(
                new VaultIndexEntry(
                    secret.Id,
                    secret.Name!,
                    secret.Description,
                    string.Join(',', secret.Tags)));
            await this.SaveEncryptedCompressedObjectAsync(
                secret,
                vault.Info.Id,
                $"secrets/{secret.Id}",
                base64Key,
                null,
                cancellationToken);

            if (updating)
            {
                results.SecretsUpdated++;
            }
            else
            {
                results.SecretsCreated++;
            }
        }

        foreach (var secret in vault.SecretsToDelete)
        {
            var existing = vault.Index.Entries.SingleOrDefault(x => x.Id == secret);
            if (existing == null)
            {
                continue;
            }

            vault.Index.Entries.Remove(existing);
            await this.DeleteSecretAsync(
                vault.Info.Id,
                secret,
                base64Key,
                cancellationToken);

            results.SecretsDeleted++;
        }

        vault.MakeClean();
        await this.SaveIndexAsync(
            vault.Info,
            vault.Index,
            base64Key,
            cancellationToken);
        results.Success = true;
        return results;
    }

    /// <summary>
    /// Loads a vault from encrypted cloud storage using the specified ID and encryption key.
    /// </summary>
    /// <param name="id">The unique identifier of the vault to load.</param>
    /// <param name="base64Key">The base64-encoded encryption key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The loaded vault instance.</returns>
    public async Task<Vault> LoadAsync(
        string id,
        string base64Key,
        CancellationToken cancellationToken)
    {
        var manifest = await this.LoadManifestAsync(
            id,
            cancellationToken);

        var info = await this.LoadInfoAsync(
            id,
            base64Key,
            cancellationToken);

        var index = await this.LoadIndexAsync(
            id,
            base64Key,
            cancellationToken);

        var vault = new Vault(
            manifest,
            info,
            index);
        vault.MakeClean();
        return vault;
    }

    /// <summary>
    /// Deletes the entire vault and all its secrets from encrypted cloud storage.
    /// </summary>
    /// <param name="vault">The vault to delete.</param>
    /// <param name="base64Key">The base64-encoded encryption key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task DeleteAsync(
        IVault vault,
        string base64Key,
        CancellationToken cancellationToken)
    {
        await this.LoadInfoAsync(
            vault.Info.Id,
            base64Key,
            cancellationToken);
        var allKeys = await this.encryptedCloudStorageProvider.ListObjectsAsync(
            $"{this.prefix}/{vault.Info.Id}/",
            null,
            cancellationToken);
        foreach (var key in allKeys)
        {
            var deleted = await this.encryptedCloudStorageProvider.DeleteEncryptedObjectAsync(
                key,
                base64Key,
                cancellationToken);
            if (!deleted)
            {
                throw new CloudStorageProviderException($"Failed to delete '{key}' from S3, while deleting vault '{vault.Info.Name}'.");
            }
        }
    }

    /// <summary>
    /// Retrieves a specific secret from the vault by its ID.
    /// </summary>
    /// <param name="vault">The vault containing the secret.</param>
    /// <param name="secretId">The unique identifier of the secret to retrieve.</param>
    /// <param name="base64Key">The base64-encoded encryption key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The secret if found; otherwise, null.</returns>
    public async Task<Secret?> GetSecretAsync(
        IVault vault,
        string secretId,
        string base64Key,
        CancellationToken cancellationToken)
    {
        var secret = await this.LoadEncryptedCompressedObjectAsync<Secret>(
            vault.Info.Id,
            $"secrets/{secretId}",
            base64Key,
            cancellationToken);
        if (secret == null)
        {
            return null;
        }

        switch (secret.SecretType)
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

    /// <summary>
    /// Verifies the integrity of the vault by checking consistency between the index and stored secrets.
    /// </summary>
    /// <param name="vault">The vault to verify.</param>
    /// <param name="base64Key">The base64-encoded encryption key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The results of the verification operation.</returns>
    public async Task<VaultVerifyResults> VerifyAsync(
        IVault vault,
        string base64Key,
        CancellationToken cancellationToken)
    {
        var results = new VaultVerifyResults();
        foreach (var index in vault.Index.Entries)
        {
            var secret = await this.GetSecretAsync(
                vault,
                index.Id,
                base64Key,
                cancellationToken);
            if (secret == null)
            {
                results.MissingSecrets++;
                continue;
            }

            if (!index.Equals(secret))
            {
                results.MismatchedSecrets++;
                continue;
            }
        }

        var secretsPrefix = $"{this.prefix}/{vault.Info.Id}/secrets/";
        var allSecrets = await this.encryptedCloudStorageProvider.ListObjectsAsync(
            secretsPrefix,
            null,
            cancellationToken);
        var allSecretKeys = allSecrets.Select(x => x.Split('/')[3]).ToList();
        var unindexedSecrets = allSecretKeys.Where(x => !vault.Index.Entries.Any(y => y.Id == x));
        results.UnindexedSecrets.AddRange(unindexedSecrets);
        return results;
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the VaultManager and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!this.disposed)
        {
            if (disposing)
            {
                this.keyDerivationService?.Dispose();
            }

            this.disposed = true;
        }
    }

    private async Task SaveManifestAsync(
        VaultInfo vaultInfo,
        VaultManifest vaultManifest,
        CancellationToken cancellationToken)
    {
        await this.SavePlainTextObjectAsync(
            vaultManifest,
            vaultInfo.Id,
            "manifest.json",
            null,
            cancellationToken);
    }

    private async Task SaveIndexAsync(
        VaultInfo vaultInfo,
        VaultIndex vaultIndex,
        string base64Key,
        CancellationToken cancellationToken)
    {
        await this.SaveEncryptedCompressedObjectAsync(
            vaultIndex,
            vaultInfo.Id,
            "index.json",
            base64Key,
            null,
            cancellationToken);
    }

    private async Task SaveInfoAsync(
        VaultInfo vaultInfo,
        string base64Key,
        Dictionary<string, string>? metaData,
        CancellationToken cancellationToken)
    {
        var metaDataCollection = new MetadataCollection();
        if (metaData != null)
        {
            foreach (var curKey in metaData.Keys)
            {
                metaDataCollection.Add($"clypse-{curKey.ToLower()}", metaData[curKey]);
            }
        }

        await this.SaveEncryptedCompressedObjectAsync(
            vaultInfo,
            vaultInfo.Id,
            "info.json",
            base64Key,
            metaDataCollection,
            cancellationToken);
    }

    private async Task<VaultManifest> LoadManifestAsync(
        string id,
        CancellationToken cancellationToken)
    {
        var manifest = await this.LoadPlainTextObjectAsync<VaultManifest>(
            id,
            "manifest.json",
            cancellationToken);
        if (manifest == null)
        {
            throw new FailedToLoadVaultInfoException($"Failed to load manifest for vault '{id}'.");
        }

        return manifest!;
    }

    private async Task<VaultInfo> LoadInfoAsync(
        string id,
        string base64Key,
        CancellationToken cancellationToken)
    {
        var info = await this.LoadEncryptedCompressedObjectAsync<VaultInfo>(
            id,
            "info.json",
            base64Key,
            cancellationToken);
        if (info == null)
        {
            throw new FailedToLoadVaultInfoException($"Failed to load Info for vault '{id}'.");
        }

        return info!;
    }

    private async Task<VaultIndex> LoadIndexAsync(
        string id,
        string base64Key,
        CancellationToken cancellationToken)
    {
        var index = await this.LoadEncryptedCompressedObjectAsync<VaultIndex>(
            id,
            "index.json",
            base64Key,
            cancellationToken);
        if (index == null)
        {
            throw new FailedToLoadVaultIndexException($"Failed to load Index for vault '{id}'.");
        }

        return index!;
    }

    private async Task SavePlainTextObjectAsync(
        object obj,
        string vaultId,
        string key,
        MetadataCollection? metaData,
        CancellationToken cancellationToken)
    {
        var objectKey = $"{this.prefix}/{vaultId}/{key}";
        var objectStream = new MemoryStream();
        await JsonSerializer.SerializeAsync(
            objectStream,
            obj,
            this.jsonSerializerOptions,
            cancellationToken);
        objectStream.Seek(0, SeekOrigin.Begin);

        var cloudStorageProvider = this.encryptedCloudStorageProvider.InnerProvider;
        await cloudStorageProvider!.PutObjectAsync(
            objectKey,
            objectStream,
            metaData,
            cancellationToken);
    }

    private async Task SaveEncryptedCompressedObjectAsync(
        object obj,
        string vaultId,
        string key,
        string base64Key,
        MetadataCollection? metaData,
        CancellationToken cancellationToken)
    {
        var objectKey = $"{this.prefix}/{vaultId}/{key}";
        var objectStream = new MemoryStream();
        await JsonSerializer.SerializeAsync(
            objectStream,
            obj,
            this.jsonSerializerOptions,
            cancellationToken);
        objectStream.Seek(0, SeekOrigin.Begin);

        var compressedObject = new MemoryStream();
        await this.compressionService.CompressAsync(
            objectStream,
            compressedObject,
            cancellationToken);
        compressedObject.Seek(0, SeekOrigin.Begin);

        await this.encryptedCloudStorageProvider.PutEncryptedObjectAsync(
            objectKey,
            compressedObject,
            base64Key,
            metaData,
            cancellationToken);
    }

    private async Task<T?> LoadPlainTextObjectAsync<T>(
        string vaultId,
        string key,
        CancellationToken cancellationToken)
    {
        var objectKey = $"{this.prefix}/{vaultId}/{key}";

        var cloudStorageProvider = this.encryptedCloudStorageProvider.InnerProvider;
        var plainTextStream = await cloudStorageProvider!.GetObjectAsync(
            objectKey,
            cancellationToken);
        if (plainTextStream == null)
        {
            return default;
        }

        var value = await JsonSerializer.DeserializeAsync<T>(
            plainTextStream,
            this.jsonSerializerOptions,
            cancellationToken);

        return value!;
    }

    private async Task<T?> LoadEncryptedCompressedObjectAsync<T>(
        string vaultId,
        string key,
        string base64Key,
        CancellationToken cancellationToken)
    {
        var objectKey = $"{this.prefix}/{vaultId}/{key}";
        var encryptedCompressedStream = await this.encryptedCloudStorageProvider.GetEncryptedObjectAsync(
            objectKey,
            base64Key,
            cancellationToken);
        if (encryptedCompressedStream == null)
        {
            return default;
        }

        var decompressedStream = new MemoryStream();
        await this.compressionService.DecompressAsync(
            encryptedCompressedStream!,
            decompressedStream,
            cancellationToken);
        decompressedStream.Seek(0, SeekOrigin.Begin);

        var value = await JsonSerializer.DeserializeAsync<T>(
            decompressedStream,
            this.jsonSerializerOptions,
            cancellationToken);

        return value!;
    }

    private async Task<bool> DeleteSecretAsync(
        string vaultId,
        string secretId,
        string base64Key,
        CancellationToken cancellationToken)
    {
        var objectKey = $"{this.prefix}/{vaultId}/secrets/{secretId}";
        var deleted = await this.encryptedCloudStorageProvider.DeleteEncryptedObjectAsync(
            objectKey,
            base64Key,
            cancellationToken);
        return deleted!;
    }

    private VaultManifest CreateManifest()
    {
        var manifest = new VaultManifest
        {
            ClypseCoreVersion = System.Reflection.Assembly
                .GetExecutingAssembly()
                .GetName()
                .Version?
                .ToString(),
            CompressionServiceName = this.compressionService.GetType().Name,
            CryptoServiceName = this.encryptedCloudStorageProvider.InnerCryptoServiceProvider?.GetType().Name,
            EncryptedCloudStorageProviderName = this.encryptedCloudStorageProvider.GetType().Name,
        };
        foreach (var param in this.keyDerivationService.Options.Parameters)
        {
            manifest.Parameters.Add($"KeyDerivationService_{param.Key}", param.Value);
        }

        return manifest;
    }

    /// <summary>
    /// Throws an ObjectDisposedException if the service has been disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(this.disposed, nameof(VaultManager));
    }
}