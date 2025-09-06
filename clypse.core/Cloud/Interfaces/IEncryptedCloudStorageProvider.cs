using Amazon.S3.Model;
using clypse.core.Cryptogtaphy.Interfaces;

namespace clypse.core.Cloud.Interfaces;

/// <summary>
/// Defines the contract for encrypted cloud storage operations with built-in encryption and decryption capabilities.
/// </summary>
public interface IEncryptedCloudStorageProvider
{
    /// <summary>
    /// Gets the inner cloud storage provider used for actual storage operations.
    /// </summary>
    public ICloudStorageProvider InnerProvider { get; }

    /// <summary>
    /// Gets the inner cryptographic service used for encryption and decryption operations.
    /// </summary>
    public ICryptoService? InnerCryptoServiceProvider { get; }

    /// <summary>
    /// Retrieves and decrypts an encrypted object from cloud storage.
    /// </summary>
    /// <param name="key">The unique key identifying the object.</param>
    /// <param name="base64EncryptionKey">The base64-encoded encryption key for decryption.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A stream containing the decrypted object data if found; otherwise, null.</returns>
    public Task<Stream?> GetEncryptedObjectAsync(
        string key,
        string base64EncryptionKey,
        CancellationToken cancellationToken);

    /// <summary>
    /// Encrypts and stores an object in cloud storage.
    /// </summary>
    /// <param name="key">The unique key to identify the object.</param>
    /// <param name="data">The stream containing the object data to encrypt.</param>
    /// <param name="base64EncryptionKey">The base64-encoded encryption key for encryption.</param>
    /// <param name="metaData">Optional metadata to associate with the object.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the object was successfully encrypted and stored; otherwise, false.</returns>
    public Task<bool> PutEncryptedObjectAsync(
        string key,
        Stream data,
        string base64EncryptionKey,
        MetadataCollection? metaData,
        CancellationToken cancellationToken);

    /// <summary>
    /// Lists all objects in cloud storage that match the specified prefix.
    /// </summary>
    /// <param name="prefix">The prefix to filter objects by.</param>
    /// <param name="delimiter">Delimiter used to separate keys.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of object keys that match the prefix.</returns>
    public Task<List<string>> ListObjectsAsync(
        string prefix,
        string? delimiter,
        CancellationToken cancellationToken);

    /// <summary>
    /// Deletes an encrypted object from cloud storage.
    /// </summary>
    /// <param name="key">The unique key identifying the object to delete.</param>
    /// <param name="base64EncryptionKey">The base64-encoded encryption key for verification.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the object was successfully deleted; otherwise, false.</returns>
    public Task<bool> DeleteEncryptedObjectAsync(
        string key,
        string base64EncryptionKey,
        CancellationToken cancellationToken);
}
