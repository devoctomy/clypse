using Amazon.S3;
using Amazon.S3.Transfer;
using System.Diagnostics.CodeAnalysis;

namespace clypse.portal.setup.Services.Upload;

/// <inheritdoc cref="IDirectoryUploadService" />
[ExcludeFromCodeCoverage(Justification = "AWS SDK wrapper - no logic to test")]
public class AmazonS3TransferUtilityDirectoryUploadService(IAmazonS3 amazonS3) : IDirectoryUploadService
{
    /// <inheritdoc />
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
