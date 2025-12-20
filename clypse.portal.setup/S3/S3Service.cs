using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;

namespace clypse.portal.setup.S3;

public class S3Service(
    IAmazonS3 amazonS3,
    AwsServiceOptions options,
    ILogger<S3Service> logger) : IS3Service
{
    public async Task<bool> CreateBucketAsync(
        string bucketName,
        CancellationToken cancellationToken = default)
    {
        var bucketNameWithPrefix = $"{options.ResourcePrefix}.{bucketName}";
        logger.LogInformation("Creating S3 bucket: {BucketName}", bucketNameWithPrefix);
        var putBucketRequest = new PutBucketRequest
        {
            BucketName = bucketNameWithPrefix
        };
        var response = await amazonS3.PutBucketAsync(putBucketRequest, cancellationToken);
        return
            response.HttpStatusCode == System.Net.HttpStatusCode.OK ||
            response.HttpStatusCode == System.Net.HttpStatusCode.Created;
    }
}
