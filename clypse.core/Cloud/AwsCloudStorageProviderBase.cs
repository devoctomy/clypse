using Amazon.S3;
using Amazon.S3.Model;
using clypse.core.Cloud.Aws.S3;
using clypse.core.Cloud.Exceptions;
using clypse.core.Cloud.Interfaces;

namespace clypse.core.Cloud;

public class AwsCloudStorageProviderBase : ICloudStorageProvider
{
    private readonly string bucketName;
    private readonly IAmazonS3Client amazonS3Client;

    public AwsCloudStorageProviderBase(
        string bucketName,
        IAmazonS3Client amazonS3Client)
    {
        this.bucketName = bucketName;
        this.amazonS3Client = amazonS3Client;
    }

    public async Task<bool> DeleteObjectAsync(
        string key,
        CancellationToken cancellationToken)
    {
        return await this.DeleteObjectAsync(
            key,
            null,
            null,
            cancellationToken);
    }

    public async Task<Stream?> GetObjectAsync(
        string key,
        CancellationToken cancellationToken)
    {
        return await this.GetObjectAsync(
            key,
            null,
            null,
            cancellationToken);
    }

    public async Task<List<string>> ListObjectsAsync(
        string prefix,
        CancellationToken cancellationToken)
    {
        return await this.ListObjectsAsync(
            prefix,
            null,
            cancellationToken);
    }

    public async Task<bool> PutObjectAsync(
        string key,
        Stream data,
        CancellationToken cancellationToken)
    {
        return await this.PutObjectAsync(
            key,
            data,
            null,
            cancellationToken);
    }

    protected async Task<Stream?> GetObjectAsync(
        string key,
        Action<GetObjectRequest>? beforeGetObjectAsync,
        Func<GetObjectResponse, Task<Stream>>? processGetObjectResponse,
        CancellationToken cancellationToken)
    {
        var getObjectRequest = new GetObjectRequest
        {
            BucketName = this.bucketName,
            Key = key,
        };

        try
        {
            beforeGetObjectAsync?.Invoke(getObjectRequest);
            GetObjectResponse getObjectResponse = await this.amazonS3Client.GetObjectAsync(getObjectRequest, cancellationToken);
            return processGetObjectResponse != null ? await processGetObjectResponse(getObjectResponse) : getObjectResponse.ResponseStream;
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

    protected async Task<List<string>> ListObjectsAsync(
        string prefix,
        Action<ListObjectsV2Request>? beforeListObjectsV2Async,
        CancellationToken cancellationToken)
    {
        var listObjectsRequest = new ListObjectsV2Request
        {
            BucketName = this.bucketName,
            Prefix = prefix,
            MaxKeys = 100,
        };

        try
        {
            var allKeys = new List<string>();
            var listObjectsResponse = default(ListObjectsV2Response);
            do
            {
                beforeListObjectsV2Async?.Invoke(listObjectsRequest);
                listObjectsResponse = await this.amazonS3Client.ListObjectsV2Async(listObjectsRequest, cancellationToken);
                allKeys.AddRange(listObjectsResponse.S3Objects.Select(x => x.Key));
                listObjectsRequest.ContinuationToken = listObjectsResponse.NextContinuationToken;
            }
            while (listObjectsResponse.IsTruncated.GetValueOrDefault());

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
        Func<PutObjectRequest, Task>? beforePutObjectAsync,
        CancellationToken cancellationToken)
    {
        var putObjectRequest = new PutObjectRequest
        {
            BucketName = this.bucketName,
            Key = key,
            InputStream = data,
        };

        try
        {
            await (beforePutObjectAsync?.Invoke(putObjectRequest) ?? Task.CompletedTask);
            await this.amazonS3Client.PutObjectAsync(putObjectRequest, cancellationToken);
            Console.WriteLine($"Successfully uploaded {key} to {this.bucketName}.");
            return true;
        }
        catch (AmazonS3Exception ex)
        {
            throw new CloudStorageProviderException($"Failed to put object with key '{key}'.", ex);
        }
    }

    protected async Task<bool> DeleteObjectAsync(
        string key,
        Action<GetObjectMetadataRequest>? beforeGetObjectMetadataAsync,
        Action<DeleteObjectRequest>? beforeDeleteObjectAsync,
        CancellationToken cancellationToken)
    {
        var getObjectMetadata = new GetObjectMetadataRequest
        {
            BucketName = this.bucketName,
            Key = key,
        };
        var deleteRequest = new DeleteObjectRequest
        {
            BucketName = this.bucketName,
            Key = key,
        };

        try
        {
            beforeGetObjectMetadataAsync?.Invoke(getObjectMetadata);
            var getMetaDataResponse = await this.amazonS3Client.GetObjectMetadataAsync(getObjectMetadata, cancellationToken);

            beforeDeleteObjectAsync?.Invoke(deleteRequest);
            var deleteResponse = await this.amazonS3Client.DeleteObjectAsync(deleteRequest, cancellationToken);
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
}
