using Amazon.S3.Model;

namespace clypse.core.Cloud.Aws.S3;

/// <summary>
/// Defines the contract for Amazon S3 client operations, providing core S3 functionality for object storage operations.
/// </summary>
public interface IAmazonS3Client
{
    /// <summary>
    /// Retrieves metadata for an object stored in Amazon S3 without downloading the object itself.
    /// </summary>
    /// <param name="request">The request containing the bucket name and object key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A response containing the object metadata.</returns>
    Task<GetObjectMetadataResponse> GetObjectMetadataAsync(
        GetObjectMetadataRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Deletes an object from Amazon S3.
    /// </summary>
    /// <param name="request">The request containing the bucket name and object key to delete.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A response indicating the result of the delete operation.</returns>
    Task<DeleteObjectResponse> DeleteObjectAsync(
        DeleteObjectRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves an object from Amazon S3, including both metadata and the object data.
    /// </summary>
    /// <param name="request">The request containing the bucket name and object key to retrieve.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A response containing the object data and metadata.</returns>
    Task<GetObjectResponse> GetObjectAsync(
        GetObjectRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Lists objects in an Amazon S3 bucket using the ListObjectsV2 API.
    /// </summary>
    /// <param name="request">The request containing the bucket name and optional filtering parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A response containing the list of objects that match the request criteria.</returns>
    Task<ListObjectsV2Response> ListObjectsV2Async(
        ListObjectsV2Request request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Stores an object in Amazon S3.
    /// </summary>
    /// <param name="request">The request containing the bucket name, object key, and data to store.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A response indicating the result of the put operation.</returns>
    Task<PutObjectResponse> PutObjectAsync(
        PutObjectRequest request,
        CancellationToken cancellationToken);
}
