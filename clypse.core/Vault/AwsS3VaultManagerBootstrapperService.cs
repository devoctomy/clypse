using System.Text.Json;
using System.Text.Json.Serialization;
using clypse.core.Cloud.Interfaces;
using clypse.core.Compression;
using clypse.core.Compression.Interfaces;
using clypse.core.Cryptography;
using clypse.core.Cryptography.Interfaces;
using clypse.core.Json;
using clypse.core.Vault.Exceptions;

namespace clypse.core.Vault;

/// <summary>
/// Bootstrapper service for creating vault managers that interact with AWS S3 for storage.
/// </summary>
public class AwsS3VaultManagerBootstrapperService(
    string prefix,
    ICloudStorageProvider awsCloudStorageProvider)
    : IVaultManagerBootstrapperService
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

    /// <summary>
    /// Creates a vault manager which is suitable for use with a specific vault identified by its Id.
    /// </summary>
    /// <param name="id">The unique identifier of the vault.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>On success returns an instance of IVaultManager suitable for use on the specified vault, otherwise null.</returns>
    public async Task<IVaultManager?> CreateVaultManagerForVaultAsync(
        string id,
        CancellationToken cancellationToken)
    {
        var awsEncryptedCloudStorageProviderTransformer = awsCloudStorageProvider as IAwsEncryptedCloudStorageProviderTransformer;
        if (awsEncryptedCloudStorageProviderTransformer == null)
        {
            throw new CloudStorageProviderDoesNotImplementIAwsEncryptedCloudStorageProviderTransformerException(awsCloudStorageProvider);
        }

        var manifest = await this.LoadManifestAsync(id, cancellationToken);
        var keyDerivationServiceOptions = new KeyDerivationServiceOptions();
        foreach (var param in manifest.Parameters)
        {
            var key = param.Key.Replace("KeyDerivationService_", string.Empty);
            keyDerivationServiceOptions.Parameters.Add(key, param.Value);
        }

        var keyDerivationServiceForVault = new KeyDerivationService(
            new RandomGeneratorService(),
            keyDerivationServiceOptions);

        ICompressionService compressionServiceForVault;
        switch (manifest.CompressionServiceName)
        {
            case "GZipCompressionService":
                compressionServiceForVault = new GZipCompressionService();
                break;

            default:
                throw new CompressionServiceNotSupportedByVaultManagerBootstrapperException(manifest.CompressionServiceName);
        }

        ICryptoService? cryptoServiceForVault = null;
        if (!string.IsNullOrEmpty(manifest.CryptoServiceName))
        {
            switch (manifest.CryptoServiceName)
            {
                case "NativeAesGcmCryptoService":
                    cryptoServiceForVault = new NativeAesGcmCryptoService();
                    break;

                case "BouncyCastleAesGcmCryptoService":
                    cryptoServiceForVault = new BouncyCastleAesGcmCryptoService();
                    break;

                default:
                    throw new CryptoServiceNotSupportedByVaultManagerBootstrapperException(manifest.CryptoServiceName);
            }
        }

        IEncryptedCloudStorageProvider encryptedCloudStorageProviderForVault;
        switch (manifest.EncryptedCloudStorageProviderName)
        {
            case "AwsS3SseCloudStorageProvider":
                encryptedCloudStorageProviderForVault = awsEncryptedCloudStorageProviderTransformer!.CreateSseProvider();
                break;

            case "AwsS3E2eCloudStorageProvider":
                encryptedCloudStorageProviderForVault = awsEncryptedCloudStorageProviderTransformer!.CreateE2eProvider(cryptoServiceForVault!);
                break;

            default:
                throw new EncryptedCloudStorageProviderNotSupportedByVaultManagerBootstrapperException(manifest.EncryptedCloudStorageProviderName);
        }

        return new VaultManager(
            prefix,
            keyDerivationServiceForVault,
            compressionServiceForVault,
            encryptedCloudStorageProviderForVault);
    }

    /// <summary>
    /// Fetches a list of all vaults available in storage.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>List of vaults found in storage with their associated manifest.</returns>
    public async Task<List<VaultListing>> ListVaultsAsync(CancellationToken cancellationToken)
    {
        var allObjectsPrefix = $"{prefix}/";
        var allObjects = await awsCloudStorageProvider.ListObjectsAsync(
            allObjectsPrefix,
            "/",
            cancellationToken);

        var vaultListings = new List<VaultListing>();
        foreach (var curVaultId in allObjects)
        {
            var manifest = await this.LoadManifestAsync(
                curVaultId,
                cancellationToken);
            if (manifest != null)
            {
                vaultListings.Add(new VaultListing
                {
                    Id = curVaultId,
                    Manifest = manifest,
                });
            }
        }

        return vaultListings;
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

    private async Task<T?> LoadPlainTextObjectAsync<T>(
        string vaultId,
        string key,
        CancellationToken cancellationToken)
    {
        var objectKey = $"{prefix}/{vaultId}/{key}";

        var plainTextStream = await awsCloudStorageProvider.GetObjectAsync(
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
}
