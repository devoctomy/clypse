using Amazon.S3;
using Amazon.S3.Model;
using clypse.core.Cloud.Aws.S3;
using clypse.core.Cloud.Interfaces;

namespace clypse.core.Cloud;

/// <summary>
/// AWS S3 cloud storage provider with server-side encryption using customer-provided keys (SSE-C).
/// This implementation uses AWS S3's native server-side encryption capabilities where the customer provides the encryption key.
/// </summary>
public class AwsS3SseCCloudStorageProvider : AwsCloudStorageProviderBase, IEncryptedCloudStorageProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AwsS3SseCCloudStorageProvider"/> class with the specified S3 configuration.
    /// </summary>
    /// <param name="bucketName">The name of the S3 bucket to use for storage.</param>
    /// <param name="amazonS3Client">The Amazon S3 client for S3 operations.</param>
    public AwsS3SseCCloudStorageProvider(
        string bucketName,
        IAmazonS3Client amazonS3Client)
        : base(bucketName, amazonS3Client)
    {
    }

    /// <summary>
    /// Gets the inner cloud storage provider used for actual storage operations.
    /// </summary>
    public ICloudStorageProvider InnerProvider => this;

    /// <summary>
    /// Deletes an encrypted object from S3 using server-side encryption with customer-provided keys (SSE-C).
    /// </summary>
    /// <param name="key">The unique key identifying the object to delete.</param>
    /// <param name="base64EncryptionKey">The base64-encoded customer-provided encryption key used for SSE-C.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the object was successfully deleted; otherwise, false.</returns>
    public async Task<bool> DeleteEncryptedObjectAsync(
        string key,
        string base64EncryptionKey,
        CancellationToken cancellationToken)
    {
        void BeforeGetObjectMetadataAsync(GetObjectMetadataRequest request)
        {
            request.ServerSideEncryptionCustomerMethod = ServerSideEncryptionCustomerMethod.AES256;
            request.ServerSideEncryptionCustomerProvidedKey = base64EncryptionKey;
        }

        return await this.DeleteObjectAsync(
            key,
            BeforeGetObjectMetadataAsync,
            null,
            cancellationToken);
    }

    /// <summary>
    /// Retrieves an encrypted object from S3 using server-side encryption with customer-provided keys (SSE-C).
    /// The object is decrypted by S3 using the provided customer key before being returned.
    /// </summary>
    /// <param name="key">The unique key identifying the object to retrieve.</param>
    /// <param name="base64EncryptionKey">The base64-encoded customer-provided encryption key used for SSE-C decryption.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A stream containing the decrypted object data if found; otherwise, null.</returns>
    public async Task<Stream?> GetEncryptedObjectAsync(
        string key,
        string base64EncryptionKey,
        CancellationToken cancellationToken)
    {
        void BeforeGetObjectAsync(GetObjectRequest request)
        {
            request.ServerSideEncryptionCustomerMethod = ServerSideEncryptionCustomerMethod.AES256;
            request.ServerSideEncryptionCustomerProvidedKey = base64EncryptionKey;
        }

        Task<Stream> ProcessGetObjectResponse(GetObjectResponse response)
        {
            return Task.FromResult(response.ResponseStream);
        }

        return await this.GetObjectAsync(
            key,
            BeforeGetObjectAsync,
            ProcessGetObjectResponse,
            cancellationToken);
    }

    /// <summary>
    /// Lists all objects in the S3 bucket that match the specified prefix.
    /// This operation does not require encryption keys as it only returns object metadata.
    /// </summary>
    /// <param name="prefix">The prefix to filter objects by.</param>
    /// <param name="delimiter">Delimiter used to separate keys.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of object keys that match the prefix.</returns>
    public new async Task<List<string>> ListObjectsAsync(
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
    /// Stores an object in S3 with server-side encryption using customer-provided keys (SSE-C).
    /// The object is encrypted by S3 using the provided customer key before being stored.
    /// </summary>
    /// <param name="key">The unique key to identify the object.</param>
    /// <param name="data">The stream containing the object data to encrypt and store.</param>
    /// <param name="base64EncryptionKey">The base64-encoded customer-provided encryption key used for SSE-C encryption.</param>
    /// <param name="metaData">Optional metadata to associate with the object.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the object was successfully encrypted and stored; otherwise, false.</returns>
    public async Task<bool> PutEncryptedObjectAsync(
        string key,
        Stream data,
        string base64EncryptionKey,
        MetadataCollection? metaData,
        CancellationToken cancellationToken)
    {
        Task BeforePutObjectAsync(PutObjectRequest request)
        {
            request.ServerSideEncryptionCustomerMethod = ServerSideEncryptionCustomerMethod.AES256;
            request.ServerSideEncryptionCustomerProvidedKey = base64EncryptionKey;
            return Task.CompletedTask;
        }

        return await this.PutObjectAsync(
            key,
            data,
            metaData,
            BeforePutObjectAsync,
            cancellationToken);
    }
}
