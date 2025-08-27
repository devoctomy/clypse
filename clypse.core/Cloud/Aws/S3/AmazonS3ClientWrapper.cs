using System.Diagnostics.CodeAnalysis;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;

namespace clypse.core.Cloud.Aws.S3;

[ExcludeFromCodeCoverage(Justification = "This is just a wrapper for AmazonS3Client.")]
public class AmazonS3ClientWrapper : IAmazonS3Client
{
    private readonly string accessKey;
    private readonly string secretAccessKey;
    private readonly Amazon.RegionEndpoint regionEndpoint;
    private readonly AmazonS3Client client;

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

    public async Task<DeleteObjectResponse> DeleteObjectAsync(
        DeleteObjectRequest request,
        CancellationToken cancellationToken)
    {
        return await this.client.DeleteObjectAsync(request, cancellationToken);
    }

    public async Task<GetObjectResponse> GetObjectAsync(
        GetObjectRequest request,
        CancellationToken cancellationToken)
    {
        return await this.client.GetObjectAsync(request, cancellationToken);
    }

    public async Task<GetObjectMetadataResponse> GetObjectMetadataAsync(
        GetObjectMetadataRequest request,
        CancellationToken cancellationToken)
    {
        return await this.client.GetObjectMetadataAsync(request, cancellationToken);
    }

    public async Task<ListObjectsV2Response> ListObjectsV2Async(
        ListObjectsV2Request request,
        CancellationToken cancellationToken)
    {
        return await this.client.ListObjectsV2Async(request, cancellationToken);
    }

    public async Task<PutObjectResponse> PutObjectAsync(
        PutObjectRequest request,
        CancellationToken cancellationToken)
    {
        return await this.client.PutObjectAsync(request, cancellationToken);
    }

    private BasicAWSCredentials CreateBasicAwsCredentials()
    {
        return new BasicAWSCredentials(
            this.accessKey,
            this.secretAccessKey);
    }
}
