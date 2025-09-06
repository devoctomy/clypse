using System.Text.Json;
using System.Text.Json.Serialization;
using clypse.core.Cloud;
using clypse.core.Cryptogtaphy;
using clypse.core.Vault.Exceptions;

namespace clypse.core.Vault;

/// <summary>
/// Bootstrapper service for creating vault managers that interact with AWS S3 for storage.
/// </summary>
public class AwsS3VaultManagerBootstrapperService(
    string prefix,
    AwsCloudStorageProviderBase awsCloudStorageProviderBase) : IVaultManagerBootstrapperService
{
    private readonly JsonSerializerOptions jsonSerializerOptions = new ()
    {
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
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
        var manifest = await this.LoadManifestAsync(id, cancellationToken);
        var keyDerivationServiceOptions = new KeyDerivationServiceOptions();
        foreach (var param in manifest.Parameters)
        {
            var key = param.Key.Replace("KeyDerivationService_", string.Empty);
            keyDerivationServiceOptions.Parameters.Add(key, param.Value);
        }

        var keyDerivationServiceForVault = new KeyDerivationService(keyDerivationServiceOptions);

        // TODO: Need to finish setting up the vault manager for the vault
        return null;
    }

    /// <summary>
    /// Fetches a list of all vault Ids available in storage.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>List of vault ids found in storage.</returns>
    public async Task<List<string>> ListVaultIdsAsync(CancellationToken cancellationToken)
    {
        var allObjectsPrefix = $"{prefix}/";
        var allObjects = await awsCloudStorageProviderBase.ListObjectsAsync(
            allObjectsPrefix,
            "/",
            cancellationToken);
        return allObjects;
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

        var plainTextStream = await awsCloudStorageProviderBase.GetObjectAsync(
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
