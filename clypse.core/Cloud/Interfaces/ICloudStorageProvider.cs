namespace clypse.core.Cloud.Interfaces;

public interface ICloudStorageProvider
{
    public Task<Stream?> GetObjectAsync(
        string key,
        CancellationToken cancellationToken);

    public Task<bool> PutObjectAsync(
        string key,
        Stream data,
        CancellationToken cancellationToken);

    public Task<List<string>> ListObjectsAsync(
        string prefix,
        CancellationToken cancellationToken);

    public Task<bool> DeleteObjectAsync(
        string key,
        CancellationToken cancellationToken);
}
