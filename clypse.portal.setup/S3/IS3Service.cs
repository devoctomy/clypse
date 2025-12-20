using Amazon.S3.Model;

namespace clypse.portal.setup.S3;

public interface IS3Service
{
    public Task<PutBucketResponse> CreateBucket(string bucketName);
}
