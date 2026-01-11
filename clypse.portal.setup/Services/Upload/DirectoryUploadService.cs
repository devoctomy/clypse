using Amazon.S3;
using Amazon.S3.Transfer;

namespace clypse.portal.setup.Services.Upload;

public class DirectoryUploadService(IAmazonS3 amazonS3) : IDirectoryUploadService
{
    public async Task UploadDirectoryAsync(
        string bucketName,
        string directoryPath,
        CancellationToken cancellationToken = default)
    {
        using var transferUtility = new TransferUtility(amazonS3);
        var uploadDirectoryRequest = new TransferUtilityUploadDirectoryRequest
        {
            BucketName = bucketName,
            Directory = directoryPath,
            SearchOption = SearchOption.AllDirectories,
            SearchPattern = "*"
        };

        await transferUtility.UploadDirectoryAsync(uploadDirectoryRequest, cancellationToken);
    }
}
