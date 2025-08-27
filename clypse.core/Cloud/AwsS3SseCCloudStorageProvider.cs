using Amazon.S3;
using Amazon.S3.Model;
using clypse.core.Cloud.Aws.S3;
using clypse.core.Cloud.Interfaces;

namespace clypse.core.Cloud;

public class AwsS3SseCCloudStorageProvider : AwsCloudStorageProviderBase, IEncryptedCloudStorageProvider
{
    public AwsS3SseCCloudStorageProvider(
        string bucketName,
        IAmazonS3Client amazonS3Client)
        : base(bucketName, amazonS3Client)
    {
    }

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
        Task BeforePutObjectAsync(PutObjectRequest request)
        {
            request.ServerSideEncryptionCustomerMethod = ServerSideEncryptionCustomerMethod.AES256;
            request.ServerSideEncryptionCustomerProvidedKey = base64EncryptionKey;
            return Task.CompletedTask;
        }

        return await this.PutObjectAsync(
            key,
            data,
            BeforePutObjectAsync,
            cancellationToken);
    }
}
