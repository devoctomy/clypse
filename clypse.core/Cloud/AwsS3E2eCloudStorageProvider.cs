using Amazon.S3.Model;
using clypse.core.Cloud.Aws.S3;
using clypse.core.Cloud.Interfaces;
using clypse.core.Cryptogtaphy.Interfaces;

namespace clypse.core.Cloud;

public class AwsS3E2eCloudStorageProvider : AwsCloudStorageProviderBase, IEncryptedCloudStorageProvider
{
    private readonly ICryptoService cryptoService;

    public AwsS3E2eCloudStorageProvider(
        string bucketName,
        IAmazonS3Client amazonS3Client,
        ICryptoService cryptoService)
        : base(bucketName, amazonS3Client)
    {
        this.cryptoService = cryptoService;
    }

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

    public new async Task<List<string>> ListObjectsAsync(
        string prefix,
        CancellationToken cancellationToken)
    {
        return await this.ListObjectsAsync(
            prefix,
            null,
            cancellationToken);
    }

    public async Task<bool> PutEncryptedObjectAsync(
        string key,
        Stream data,
        string base64EncryptionKey,
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
            BeforePutObjectAsync,
            cancellationToken);
    }
}
