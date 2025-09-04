using Amazon.S3.Model;
using clypse.core.Cloud.Aws.S3;
using clypse.core.Cloud.Interfaces;
using clypse.core.Cryptogtaphy.Interfaces;

namespace clypse.core.Cloud;

/// <summary>
/// AWS S3 cloud storage provider with end-to-end encryption capabilities, providing encrypted storage and retrieval of objects.
/// </summary>
public class AwsS3E2eCloudStorageProvider : AwsCloudStorageProviderBase, IEncryptedCloudStorageProvider
{
    private readonly ICryptoService cryptoService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AwsS3E2eCloudStorageProvider"/> class with the specified S3 configuration and crypto service.
    /// </summary>
    /// <param name="bucketName">The name of the S3 bucket to use for storage.</param>
    /// <param name="amazonS3Client">The Amazon S3 client for S3 operations.</param>
    /// <param name="cryptoService">The cryptographic service for encryption and decryption.</param>
    public AwsS3E2eCloudStorageProvider(
        string bucketName,
        IAmazonS3Client amazonS3Client,
        ICryptoService cryptoService)
        : base(bucketName, amazonS3Client)
    {
        this.cryptoService = cryptoService;
    }

    /// <summary>
    /// Deletes an encrypted object from S3. Since the object is stored encrypted, no decryption key is needed for deletion.
    /// </summary>
    /// <param name="key">The unique key identifying the object to delete.</param>
    /// <param name="base64EncryptionKey">The base64-encoded encryption key (not used for deletion but kept for interface consistency).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the object was successfully deleted; otherwise, false.</returns>
    public async Task<bool> DeleteEncryptedObjectAsync(
        string key,
        string base64EncryptionKey,
        CancellationToken cancellationToken)
    {
        return await this.DeleteObjectAsync(
            key,
            null,
            null,
            cancellationToken);
    }

    /// <summary>
    /// Retrieves and decrypts an object from S3 using end-to-end encryption.
    /// The object is downloaded from S3 and then decrypted client-side using the provided encryption key.
    /// </summary>
    /// <param name="key">The unique key identifying the object to retrieve.</param>
    /// <param name="base64EncryptionKey">The base64-encoded encryption key used for client-side decryption.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A stream containing the decrypted object data if found; otherwise, null.</returns>
    public async Task<Stream?> GetEncryptedObjectAsync(
        string key,
        string base64EncryptionKey,
        CancellationToken cancellationToken)
    {
        async Task<Stream> ProcessGetObjectResponse(GetObjectResponse response)
        {
            var decrypted = new MemoryStream();
            await this.cryptoService.DecryptAsync(response.ResponseStream, decrypted, base64EncryptionKey);
            decrypted.Seek(0, SeekOrigin.Begin);
            return decrypted;
        }

        return await this.GetObjectAsync(
            key,
            null,
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
    /// Encrypts and stores an object in S3 using end-to-end encryption.
    /// The object is encrypted client-side using the provided encryption key before being uploaded to S3.
    /// </summary>
    /// <param name="key">The unique key to identify the object.</param>
    /// <param name="data">The stream containing the object data to encrypt and store.</param>
    /// <param name="base64EncryptionKey">The base64-encoded encryption key used for client-side encryption.</param>
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
        async Task BeforePutObjectAsync(PutObjectRequest request)
        {
            var encrypted = new MemoryStream();
            await this.cryptoService.EncryptAsync(data, encrypted, base64EncryptionKey);
            encrypted.Seek(0, SeekOrigin.Begin);
            request.InputStream = encrypted;
        }

        return await this.PutObjectAsync(
            key,
            data,
            metaData,
            BeforePutObjectAsync,
            cancellationToken);
    }
}
