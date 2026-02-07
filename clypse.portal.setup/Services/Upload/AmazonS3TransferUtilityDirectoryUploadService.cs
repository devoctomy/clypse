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

        // Set Content-Type for each file based on extension
        uploadDirectoryRequest.UploadDirectoryFileRequestEvent += (_, args) =>
        {
            args.UploadRequest.ContentType = GetContentType(args.UploadRequest.FilePath);
        };

        await transferUtility.UploadDirectoryAsync(uploadDirectoryRequest, cancellationToken);
    }

    private static string GetContentType(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".js" => "application/javascript",
            ".json" => "application/json",
            ".css" => "text/css",
            ".html" => "text/html",
            ".wasm" => "application/wasm",
            ".dll" => "application/octet-stream",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".ico" => "image/x-icon",
            ".svg" => "image/svg+xml",
            ".woff" => "font/woff",
            ".woff2" => "font/woff2",
            ".ttf" => "font/ttf",
            ".eot" => "application/vnd.ms-fontobject",
            ".webmanifest" => "application/manifest+json",
            ".txt" => "text/plain",
            _ => "application/octet-stream"
        };
    }
}
