namespace clypse.portal.setup.S3;

public interface IS3Service
{
    public Task<bool> CreateBucket(string bucketName);
}
