using Amazon.S3;
using Amazon.S3.Model;
using clypse.core.Cloud.Aws.S3;
using clypse.core.Cloud.Exceptions;
using clypse.core.Cloud.Interfaces;

namespace clypse.core.Cloud;

public class AwsCloudStorageProviderBase : ICloudStorageProvider
{
    private readonly string _bucketName;
    private readonly IAmazonS3Client _amazonS3Client;

    public AwsCloudStorageProviderBase(
        string bucketName,
        IAmazonS3Client amazonS3Client)
    {
        _bucketName = bucketName;
        _amazonS3Client = amazonS3Client;
    }

    protected async Task<bool> DeleteObjectAsync(
        string key,
        Action<GetObjectMetadataRequest>? BeforeGetObjectMetadataAsync,
        Action<DeleteObjectRequest>? BeforeDeleteObjectAsync,
        CancellationToken cancellationToken)
    {
        var getObjectMetadata = new GetObjectMetadataRequest
        {
            BucketName = _bucketName,
            Key = key
        };
        var deleteRequest = new DeleteObjectRequest
        {
            BucketName = _bucketName,
            Key = key
        };

        try
        {
            BeforeGetObjectMetadataAsync?.Invoke(getObjectMetadata);
            var getMetaDataResponse = await _amazonS3Client.GetObjectMetadataAsync(getObjectMetadata, cancellationToken);

            BeforeDeleteObjectAsync?.Invoke(deleteRequest);
            var deleteResponse = await _amazonS3Client.DeleteObjectAsync(deleteRequest, cancellationToken);
            return true;
        }
        catch (AmazonS3Exception ex)
        {
            if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }

            throw new CloudStorageProviderException($"Failed to delete object with key '{key}'.", ex);
        }
    }

    public async Task<bool> DeleteObjectAsync(
        string key,
        CancellationToken cancellationToken)
    {
        return await DeleteObjectAsync(
            key,
            null,
            null,
            cancellationToken);
    }

    protected async Task<Stream?> GetObjectAsync(
        string key,
        Action<GetObjectRequest>? BeforeGetObjectAsync,
        Func<GetObjectResponse, Task<Stream>>? ProcessGetObjectResponse,
        CancellationToken cancellationToken)
    {
        var getObjectRequest = new GetObjectRequest
        {
            BucketName = _bucketName,
            Key = key
        };

        try
        {
            BeforeGetObjectAsync?.Invoke(getObjectRequest);
            GetObjectResponse getObjectResponse = await _amazonS3Client.GetObjectAsync(getObjectRequest, cancellationToken);               
            return ProcessGetObjectResponse != null ? await ProcessGetObjectResponse(getObjectResponse) : getObjectResponse.ResponseStream;
        }
        catch (AmazonS3Exception ex)
        {
            if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            throw new CloudStorageProviderException($"Failed to get object with key '{key}'.", ex);
        }
    }

    public async Task<Stream?> GetObjectAsync(
        string key,
        CancellationToken cancellationToken)
    {
        return await GetObjectAsync(
            key,
            null,
            null,
            cancellationToken);
    }

    public async Task<List<string>> ListObjectsAsync(
    string prefix,
    CancellationToken cancellationToken)
    {
        return await ListObjectsAsync(
            prefix,
            null,
            cancellationToken);
    }

    protected async Task<List<string>> ListObjectsAsync(
        string prefix,
        Action<ListObjectsV2Request>? BeforeListObjectsV2Async,
        CancellationToken cancellationToken)
    {
        var listObjectsRequest = new ListObjectsV2Request
        {
            BucketName = _bucketName,
            Prefix = prefix,
            MaxKeys = 100
        };

        try
        {
            var allKeys = new List<string>();
            var listObjectsResponse = default(ListObjectsV2Response);
            do
            {
                BeforeListObjectsV2Async?.Invoke(listObjectsRequest);
                listObjectsResponse = await _amazonS3Client.ListObjectsV2Async(listObjectsRequest, cancellationToken);
                allKeys.AddRange(listObjectsResponse.S3Objects.Select(x => x.Key));
                listObjectsRequest.ContinuationToken = listObjectsResponse.NextContinuationToken;
            } while (listObjectsResponse.IsTruncated.GetValueOrDefault());

            return allKeys;
        }
        catch (AmazonS3Exception ex)
        {
            throw new CloudStorageProviderException($"Failed to list objects with prefix '{prefix}'.", ex);
        }
    }

    protected async Task<bool> PutObjectAsync(
        string key,
        Stream data,
        Func<PutObjectRequest, Task>? BeforePutObjectAsync,
        CancellationToken cancellationToken)
    {
        var putObjectRequest = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = data
        };

        try
        {
            await (BeforePutObjectAsync?.Invoke(putObjectRequest) ?? Task.CompletedTask);
            await _amazonS3Client.PutObjectAsync(putObjectRequest, cancellationToken);
            Console.WriteLine($"Successfully uploaded {key} to {_bucketName}.");
            return true;
        }
        catch (AmazonS3Exception ex)
        {
            throw new CloudStorageProviderException($"Failed to put object with key '{key}'.", ex);
        }
    }

    public async Task<bool> PutObjectAsync(
        string key,
        Stream data,
        CancellationToken cancellationToken)
    {
        return await PutObjectAsync(
            key,
            data,
            null,
            cancellationToken);
    }
}
