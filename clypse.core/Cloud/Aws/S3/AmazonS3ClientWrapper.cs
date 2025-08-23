using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using System.Diagnostics.CodeAnalysis;

namespace clypse.core.Cloud.Aws.S3;

[ExcludeFromCodeCoverage(Justification = "This is just a wrapper for AmazonS3Client.")]
public class AmazonS3ClientWrapper : IAmazonS3Client
{
    private readonly string _accessKey;
    private readonly string _secretAccessKey;
    private readonly Amazon.RegionEndpoint _regionEndpoint;
    private readonly AmazonS3Client _client;

    public AmazonS3ClientWrapper(
        string accessKey,
        string secretAccessKey,
        Amazon.RegionEndpoint regionEndpoint)
    {
        _accessKey = accessKey;
        _secretAccessKey = secretAccessKey;
        _regionEndpoint = regionEndpoint;
        _client = new AmazonS3Client(CreateBasicAwsCredentials(), _regionEndpoint);
    }

    public async Task<DeleteObjectResponse> DeleteObjectAsync(
        DeleteObjectRequest request,
        CancellationToken cancellationToken)
    {
        return await _client.DeleteObjectAsync(request, cancellationToken);
    }

    public async Task<GetObjectResponse> GetObjectAsync(
        GetObjectRequest request,
        CancellationToken cancellationToken)
    {
        return await _client.GetObjectAsync(request, cancellationToken);
    }

    public async Task<GetObjectMetadataResponse> GetObjectMetadataAsync(
        GetObjectMetadataRequest request,
        CancellationToken cancellationToken)
    {
        return await _client.GetObjectMetadataAsync(request, cancellationToken);
    }

    public async Task<ListObjectsV2Response> ListObjectsV2Async(
        ListObjectsV2Request request,
        CancellationToken cancellationToken)
    {
        return await _client.ListObjectsV2Async(request, cancellationToken);
    }

    public async Task<PutObjectResponse> PutObjectAsync(
        PutObjectRequest request,
        CancellationToken cancellationToken)
    {
        return await _client.PutObjectAsync(request, cancellationToken);
    }

    private BasicAWSCredentials CreateBasicAwsCredentials()
    {
        return new BasicAWSCredentials(
            _accessKey,
            _secretAccessKey);
    }
}
