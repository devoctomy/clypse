namespace clypse.core.Cloud.Interfaces;

public interface IEncryptedCloudStorageProvider
{
    public Task<Stream?> GetEncryptedObjectAsync(
        string key,
        string base64EncryptionKey,
        CancellationToken cancellationToken);
    public Task<bool> PutEncryptedObjectAsync(
        string key,
        Stream data,
        string base64EncryptionKey,
        CancellationToken cancellationToken);
    public Task<List<string>> ListObjectsAsync(
        string prefix,
        CancellationToken cancellationToken);
    public Task<bool> DeleteEncryptedObjectAsync(
        string key,
        string base64EncryptionKey,
        CancellationToken cancellationToken);
}
