using System.Diagnostics.CodeAnalysis;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;

namespace clypse.core.Cloud.Aws.S3;

/// <summary>
/// Wrapper implementation of IAmazonS3Client that encapsulates the AWS SDK AmazonS3Client with basic AWS credentials.
/// This wrapper provides a testable abstraction over the AWS S3 client and manages credential creation internally.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "This is just a wrapper for AmazonS3Client.")]
public class AmazonS3ClientWrapper : IAmazonS3Client
{
    private readonly string accessKey;
    private readonly string secretAccessKey;
    private readonly Amazon.RegionEndpoint regionEndpoint;
    private readonly AmazonS3Client client;

    /// <summary>
    /// Initializes a new instance of the <see cref="AmazonS3ClientWrapper"/> class with the specified AWS credentials and region.
    /// </summary>
    /// <param name="accessKey">The AWS access key for authentication.</param>
    /// <param name="secretAccessKey">The AWS secret access key for authentication.</param>
    /// <param name="regionEndpoint">The AWS region endpoint for S3 operations.</param>
    public AmazonS3ClientWrapper(
        string accessKey,
        string secretAccessKey,
        Amazon.RegionEndpoint regionEndpoint)
    {
        this.accessKey = accessKey;
        this.secretAccessKey = secretAccessKey;
        this.regionEndpoint = regionEndpoint;
        this.client = new AmazonS3Client(this.CreateBasicAwsCredentials(), regionEndpoint);
    }

    /// <summary>
    /// Deletes an object from Amazon S3.
    /// </summary>
    /// <param name="request">The request containing the bucket name and object key to delete.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A response indicating the result of the delete operation.</returns>
    public async Task<DeleteObjectResponse> DeleteObjectAsync(
        DeleteObjectRequest request,
        CancellationToken cancellationToken)
    {
        return await this.client.DeleteObjectAsync(request, cancellationToken);
    }

    /// <summary>
    /// Retrieves an object from Amazon S3, including both metadata and the object data.
    /// </summary>
    /// <param name="request">The request containing the bucket name and object key to retrieve.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A response containing the object data and metadata.</returns>
    public async Task<GetObjectResponse> GetObjectAsync(
        GetObjectRequest request,
        CancellationToken cancellationToken)
    {
        return await this.client.GetObjectAsync(request, cancellationToken);
    }

    /// <summary>
    /// Retrieves metadata for an object stored in Amazon S3 without downloading the object itself.
    /// </summary>
    /// <param name="request">The request containing the bucket name and object key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A response containing the object metadata.</returns>
    public async Task<GetObjectMetadataResponse> GetObjectMetadataAsync(
        GetObjectMetadataRequest request,
        CancellationToken cancellationToken)
    {
        return await this.client.GetObjectMetadataAsync(request, cancellationToken);
    }

    /// <summary>
    /// Lists objects in an Amazon S3 bucket using the ListObjectsV2 API.
    /// </summary>
    /// <param name="request">The request containing the bucket name and optional filtering parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A response containing the list of objects that match the request criteria.</returns>
    public async Task<ListObjectsV2Response> ListObjectsV2Async(
        ListObjectsV2Request request,
        CancellationToken cancellationToken)
    {
        return await this.client.ListObjectsV2Async(request, cancellationToken);
    }

    /// <summary>
    /// Stores an object in Amazon S3.
    /// </summary>
    /// <param name="request">The request containing the bucket name, object key, and data to store.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A response indicating the result of the put operation.</returns>
    public async Task<PutObjectResponse> PutObjectAsync(
        PutObjectRequest request,
        CancellationToken cancellationToken)
    {
        return await this.client.PutObjectAsync(request, cancellationToken);
    }

    /// <summary>
    /// Creates BasicAWSCredentials using the provided access key and secret access key.
    /// </summary>
    /// <returns>A BasicAWSCredentials instance for AWS authentication.</returns>
    private BasicAWSCredentials CreateBasicAwsCredentials()
    {
        return new BasicAWSCredentials(
            this.accessKey,
            this.secretAccessKey);
    }
}
