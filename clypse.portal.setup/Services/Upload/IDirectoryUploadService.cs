namespace clypse.portal.setup.Services.Upload;

public interface IDirectoryUploadService
{
    public Task UploadDirectoryAsync(
        string bucketName,
        string directoryPath,
        CancellationToken cancellationToken = default);
}
