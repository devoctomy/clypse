namespace clypse.core.Vault;

/// <summary>
/// Contains metadata about a vault including versions and configuration parameters.
/// </summary>
public class VaultManifest
{
    /// <summary>
    /// Gets or sets the version of the clypse core library used to create this vault.
    /// </summary>
    public string? ClypseCoreVersion { get; set; }

    /// <summary>
    /// Gets or sets name of the compression service used for this vault.
    /// </summary>
    public string? CompressionServiceName { get; set; }

    /// <summary>
    /// Gets or sets name of the encrypted cloud storage provider used for this vault.
    /// </summary>
    public string? EncryptedCloudStorageProviderName { get; set; }

    /// <summary>
    /// Gets or sets Parameters used to configure the compression service, encrypted cloud storage provider, and key derivation service.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = [];
}
