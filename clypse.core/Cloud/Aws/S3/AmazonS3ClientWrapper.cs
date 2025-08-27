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
        string awsAccessKey,
        string awsSecretAccessKey,
        Amazon.RegionEndpoint awsRegionEndpoint)
    {
        accessKey = awsAccessKey;
        secretAccessKey = awsSecretAccessKey;
        regionEndpoint = awsRegionEndpoint;
        client = new AmazonS3Client(CreateBasicAwsCredentials(), regionEndpoint);
    }

    public async Task<DeleteObjectResponse> DeleteObjectAsync(
        DeleteObjectRequest request,
        CancellationToken cancellationToken)
    {
        return await client.DeleteObjectAsync(request, cancellationToken);
    }

    public async Task<GetObjectResponse> GetObjectAsync(
        GetObjectRequest request,
        CancellationToken cancellationToken)
    {
        return await client.GetObjectAsync(request, cancellationToken);
    }

    public async Task<GetObjectMetadataResponse> GetObjectMetadataAsync(
        GetObjectMetadataRequest request,
        CancellationToken cancellationToken)
    {
        return await client.GetObjectMetadataAsync(request, cancellationToken);
    }

    public async Task<ListObjectsV2Response> ListObjectsV2Async(
        ListObjectsV2Request request,
        CancellationToken cancellationToken)
    {
        return await client.ListObjectsV2Async(request, cancellationToken);
    }

    public async Task<PutObjectResponse> PutObjectAsync(
        PutObjectRequest request,
        CancellationToken cancellationToken)
    {
        return await client.PutObjectAsync(request, cancellationToken);
    }

    private BasicAWSCredentials CreateBasicAwsCredentials()
    {
        return new BasicAWSCredentials(
            accessKey,
            secretAccessKey);
    }
}
