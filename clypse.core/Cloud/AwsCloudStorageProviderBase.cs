using Amazon.S3;
using Amazon.S3.Model;
using clypse.core.Cloud.Aws.S3;
using clypse.core.Cloud.Exceptions;
using clypse.core.Cloud.Interfaces;
using clypse.core.Cryptography.Interfaces;

namespace clypse.core.Cloud;

/// <summary>
/// Base class for AWS S3 cloud storage providers, implementing common S3 operations and providing extensible hooks for derived classes.
/// This class provides the foundation for both encrypted and unencrypted S3 storage implementations.
/// </summary>
public class AwsCloudStorageProviderBase : ICloudStorageProvider, IAwsEncryptedCloudStorageProviderTransformer
{
    private readonly string bucketName;
    private readonly IAmazonS3Client amazonS3Client;

    /// <summary>
    /// Initializes a new instance of the <see cref="AwsCloudStorageProviderBase"/> class with the specified S3 configuration.
    /// </summary>
    /// <param name="bucketName">The name of the S3 bucket to use for storage operations.</param>
    /// <param name="amazonS3Client">The Amazon S3 client for performing S3 operations.</param>
    public AwsCloudStorageProviderBase(
        string bucketName,
        IAmazonS3Client amazonS3Client)
    {
        this.bucketName = bucketName;
        this.amazonS3Client = amazonS3Client;
    }

    /// <summary>
    /// Deletes an object from the S3 bucket.
    /// </summary>
    /// <param name="key">The unique key identifying the object to delete.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the object was successfully deleted; otherwise, false.</returns>
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

    /// <summary>
    /// Retrieves an object from the S3 bucket.
    /// </summary>
    /// <param name="key">The unique key identifying the object to retrieve.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A stream containing the object data if found; otherwise, null.</returns>
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

    /// <summary>
    /// Lists all objects in the S3 bucket that match the specified prefix.
    /// </summary>
    /// <param name="prefix">The prefix to filter objects by.</param>
    /// <param name="delimiter">Delimiter used to separate keys.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of object keys that match the prefix.</returns>
    public async Task<List<string>> ListObjectsAsync(
        string prefix,
        string? delimiter,
        CancellationToken cancellationToken)
    {
        return await this.ListObjectsAsync(
            prefix,
            delimiter,
            null,
            cancellationToken);
    }

    /// <summary>
    /// Stores an object in the S3 bucket.
    /// </summary>
    /// <param name="key">The unique key to identify the object.</param>
    /// <param name="data">The stream containing the object data to store.</param>
    /// <param name="metaData">Optional metadata to associate with the object.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the object was successfully stored; otherwise, false.</returns>
    public async Task<bool> PutObjectAsync(
        string key,
        Stream data,
        MetadataCollection? metaData,
        CancellationToken cancellationToken)
    {
        return await this.PutObjectAsync(
            key,
            data,
            metaData,
            null,
            cancellationToken);
    }

    /// <summary>
    /// Creates an end-to-end encrypted AWS S3 cloud storage provider using the specified cryptographic service.
    /// </summary>
    /// <param name="cryptoService">Cryptographic service to use for the encryption process.</param>
    /// <returns>Instance of AwsS3E2eCloudStorageProvider.</returns>
    public AwsS3E2eCloudStorageProvider CreateE2eProvider(ICryptoService cryptoService)
    {
        return new AwsS3E2eCloudStorageProvider(
            this.bucketName,
            this.amazonS3Client,
            cryptoService);
    }

    /// <summary>
    /// Creates an server-side encrypted AWS S3 cloud storage provider using the specified cryptographic service.
    /// </summary>
    /// <returns>Instance of AwsS3SseCCloudStorageProvider.</returns>
    public AwsS3SseCloudStorageProvider CreateSseProvider()
    {
        return new AwsS3SseCloudStorageProvider(
            this.bucketName,
            this.amazonS3Client);
    }

    /// <summary>
    /// Retrieves an object from the S3 bucket with extensibility hooks for derived classes.
    /// </summary>
    /// <param name="key">The unique key identifying the object to retrieve.</param>
    /// <param name="beforeGetObjectAsync">Optional action to modify the GetObjectRequest before execution.</param>
    /// <param name="processGetObjectResponse">Optional function to process the GetObjectResponse and return a custom stream.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A stream containing the object data if found; otherwise, null.</returns>
    /// <exception cref="CloudStorageProviderException">Thrown when the S3 operation fails.</exception>
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

    /// <summary>
    /// Lists all objects in the S3 bucket that match the specified prefix with extensibility hooks for derived classes.
    /// This method handles pagination automatically to retrieve all matching objects.
    /// </summary>
    /// <param name="prefix">The prefix to filter objects by.</param>
    /// <param name="delimiter">Delimiter used to separate keys.</param>
    /// <param name="beforeListObjectsV2Async">Optional action to modify the ListObjectsV2Request before execution.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of object keys that match the prefix.</returns>
    /// <exception cref="CloudStorageProviderException">Thrown when the S3 operation fails.</exception>
    protected async Task<List<string>> ListObjectsAsync(
        string prefix,
        string? delimiter,
        Action<ListObjectsV2Request>? beforeListObjectsV2Async,
        CancellationToken cancellationToken)
    {
        var listObjectsRequest = new ListObjectsV2Request
        {
            BucketName = this.bucketName,
            Prefix = prefix,
            MaxKeys = 100,
            Delimiter = delimiter ?? string.Empty,
        };

        try
        {
            var allKeys = new List<string>();
            var listObjectsResponse = default(ListObjectsV2Response);
            do
            {
                beforeListObjectsV2Async?.Invoke(listObjectsRequest);
                listObjectsResponse = await this.amazonS3Client.ListObjectsV2Async(listObjectsRequest, cancellationToken);
                if (listObjectsResponse.S3Objects != null && listObjectsResponse.S3Objects.Count > 0)
                {
                    allKeys.AddRange(listObjectsResponse.S3Objects.Select(x => x.Key));
                }
                else if (!string.IsNullOrEmpty(prefix) && !string.IsNullOrEmpty(delimiter))
                {
                    allKeys.AddRange(listObjectsResponse.CommonPrefixes.Select(x => x.Replace(prefix, string.Empty).TrimEnd(delimiter[0])));
                }

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

    /// <summary>
    /// Stores an object in the S3 bucket with extensibility hooks for derived classes.
    /// </summary>
    /// <param name="key">The unique key to identify the object.</param>
    /// <param name="data">The stream containing the object data to store.</param>
    /// <param name="metaData">Optional metadata to associate with the object.</param>
    /// <param name="beforePutObjectAsync">Optional asynchronous function to modify the PutObjectRequest before execution.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the object was successfully stored; otherwise, false.</returns>
    /// <exception cref="CloudStorageProviderException">Thrown when the S3 operation fails.</exception>
    protected async Task<bool> PutObjectAsync(
        string key,
        Stream data,
        MetadataCollection? metaData,
        Func<PutObjectRequest, Task>? beforePutObjectAsync,
        CancellationToken cancellationToken)
    {
        var putObjectRequest = new PutObjectRequest
        {
            BucketName = this.bucketName,
            Key = key,
            InputStream = data,
        };

        if (metaData != null)
        {
            foreach (var curKey in metaData.Keys)
            {
                putObjectRequest.Metadata.Add(curKey, metaData[curKey]);
            }
        }

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

    /// <summary>
    /// Deletes an object from the S3 bucket with extensibility hooks for derived classes.
    /// This method first checks if the object exists before attempting deletion.
    /// </summary>
    /// <param name="key">The unique key identifying the object to delete.</param>
    /// <param name="beforeGetObjectMetadataAsync">Optional action to modify the GetObjectMetadataRequest before execution.</param>
    /// <param name="beforeDeleteObjectAsync">Optional action to modify the DeleteObjectRequest before execution.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the object was successfully deleted; false if the object was not found.</returns>
    /// <exception cref="CloudStorageProviderException">Thrown when the S3 operation fails.</exception>
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
