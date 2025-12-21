namespace clypse.portal.setup.S3;

public interface IS3Service
{
    public Task<bool> CreateBucketAsync(
        string bucketName,
        CancellationToken cancellationToken = default);

    public Task<bool> SetBucketCorsConfigurationAsync(
        string bucketName,
        List<string> allowedHeaders,
        List<string> allowedMethods,
        List<string> allowedOrigins,
        CancellationToken cancellationToken = default);

    public Task<bool> SetBucketPolicyAsync(
        string bucketName,
        object policyDocument,
        CancellationToken cancellationToken = default);
}
