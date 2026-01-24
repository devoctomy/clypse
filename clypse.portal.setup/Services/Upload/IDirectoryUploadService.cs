namespace clypse.portal.setup.Services.Upload;

/// <summary>
/// Uploads directories to remote storage providers.
/// </summary>
public interface IDirectoryUploadService
{
    /// <summary>
    /// Uploads a directory recursively to the specified bucket.
    /// </summary>
    /// <param name="bucketName">Destination bucket name.</param>
    /// <param name="directoryPath">Local directory path to upload.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    public Task UploadDirectoryAsync(
        string bucketName,
        string directoryPath,
        CancellationToken cancellationToken = default);
}
