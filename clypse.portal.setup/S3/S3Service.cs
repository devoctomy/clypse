using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;

namespace clypse.portal.setup.S3;

public class S3Service(
    IAmazonS3 amazonS3,
    AwsServiceOptions options,
    ILogger<S3Service> logger) : IS3Service
{
    public async Task<PutBucketResponse> CreateBucket(string bucketName)
    {
        var bucketNameWithPrefix = $"{options.ResourcePrefix}-{bucketName}";
        logger.LogInformation("Creating S3 bucket: {BucketName}", bucketNameWithPrefix);
        var putBucketRequest = new PutBucketRequest
        {
            BucketName = bucketNameWithPrefix
        };
        return await amazonS3.PutBucketAsync(putBucketRequest);
    }
}
