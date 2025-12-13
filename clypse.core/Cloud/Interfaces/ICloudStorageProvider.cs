using Amazon.S3.Model;

namespace clypse.core.Cloud.Interfaces;

/// <summary>
/// Defines the contract for cloud storage operations including object retrieval, storage, listing, and deletion.
/// </summary>
public interface ICloudStorageProvider
{
    /// <summary>
    /// Retrieves an object from cloud storage.
    /// </summary>
    /// <param name="key">The unique key identifying the object.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A stream containing the object data if found; otherwise, null.</returns>
    public Task<Stream?> GetObjectAsync(
        string key,
        CancellationToken cancellationToken);

    /// <summary>
    /// Stores an object in cloud storage.
    /// </summary>
    /// <param name="key">The unique key to identify the object.</param>
    /// <param name="data">The stream containing the object data.</param>
    /// <param name="metaData">Optional metadata to associate with the object.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the object was successfully stored; otherwise, false.</returns>
    public Task<bool> PutObjectAsync(
        string key,
        Stream data,
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
    /// Deletes an object from cloud storage.
    /// </summary>
    /// <param name="key">The unique key identifying the object to delete.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the object was successfully deleted; otherwise, false.</returns>
    public Task<bool> DeleteObjectAsync(
        string key,
        CancellationToken cancellationToken);
}
