using Amazon.S3;

namespace clypse.portal.setup.Services.S3;

/// <summary>
/// Defines operations for managing Amazon S3 buckets and their configurations.
/// </summary>
public interface IS3Service
{
    /// <summary>
    /// Determines whether a bucket exists for the configured resource prefix and bucket name.
    /// </summary>
    /// <param name="bucketName">The bucket name without the resource prefix.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns><see langword="true"/> when the bucket exists; otherwise, <see langword="false"/>.</returns>
    public Task<bool> DoesBucketExistAsync(
        string bucketName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new S3 bucket with the specified name.
    /// </summary>
    /// <param name="bucketName">The name of the bucket to create (without the resource prefix).</param>
    /// <param name="disableBlockPublicAccess">If true, disables block public access settings for the bucket.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>True if the bucket was created successfully; otherwise, false.</returns>
    public Task<bool> CreateBucketAsync(
        string bucketName,
        bool disableBlockPublicAccess,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies tags to the specified bucket.
    /// </summary>
    /// <param name="bucketName">The bucket name without the resource prefix.</param>
    /// <param name="tags">Key/value pairs to assign.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns><see langword="true"/> when the tags are applied; otherwise, <see langword="false"/>.</returns>
    public Task<bool> SetBucketTags(
        string bucketName,
        Dictionary<string, string> tags,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the CORS configuration for the specified S3 bucket.
    /// </summary>
    /// <param name="bucketName">Name of the bucket to configure (without the resource prefix).</param>
    /// <param name="allowedHeaders">List of allowed headers in CORS requests.</param>
    /// <param name="allowedMethods">List of allowed HTTP methods.</param>
    /// <param name="allowedOrigins">List of allowed origins for CORS requests.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>True if the CORS configuration was set successfully; otherwise, false.</returns>
    public Task<bool> SetBucketCorsConfigurationAsync(
        string bucketName,
        List<string> allowedHeaders,
        List<string> allowedMethods,
        List<string> allowedOrigins,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the bucket policy for the specified S3 bucket.
    /// </summary>
    /// <param name="bucketName">Name of the bucket to configure (without the resource prefix).</param>
    /// <param name="policyDocument">The policy document as an object to be serialized to JSON.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>True if the bucket policy was set successfully; otherwise, false.</returns>
    public Task<bool> SetBucketPolicyAsync(
        string bucketName,
        object policyDocument,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Configures the specified S3 bucket to host a static website.
    /// </summary>
    /// <param name="bucketName">Name of the bucket to configure (without the resource prefix).</param>
    /// <param name="indexDocumentSuffix">The suffix for the index document.</param>
    /// <param name="errorDocument">The error document to use.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>True if the website configuration was set successfully; otherwise, false.</returns>
    public Task<bool> SetBucketWebsiteConfigurationAsync(
        string bucketName,
        string indexDocumentSuffix = "index.html",
        string errorDocument = "error.html",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the Access Control List (ACL) for the specified S3 bucket.
    /// </summary>
    /// <param name="bucketName">Name of the bucket to configure (without the resource prefix).</param>
    /// <param name="acl">Canned ACL to specify for the bucket.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>True if the ACL for the bucket was set successfully; otherwise, false.</returns>
    public Task<bool> SetBucketAcl(
        string bucketName,
        S3CannedACL acl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads all files from a directory recursively to the specified S3 bucket.
    /// </summary>
    /// <param name="bucketName">Name of the bucket to upload to (without the resource prefix).</param>
    /// <param name="directoryPath">The local directory path to upload.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>True if the directory was uploaded successfully; otherwise, false.</returns>
    public Task<bool> UploadDirectoryToBucket(
        string bucketName,
        string directoryPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads an object's data as a byte array.
    /// </summary>
    /// <param name="bucketName">The bucket name without the resource prefix.</param>
    /// <param name="objectKey">The object key within the bucket.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The object's contents as a byte array.</returns>
    public Task<byte[]> DownloadObjectDataAsync(
        string bucketName,
        string objectKey,
        CancellationToken cancellationToken = default);
}
