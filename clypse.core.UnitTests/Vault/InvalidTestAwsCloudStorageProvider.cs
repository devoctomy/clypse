using Amazon.S3.Model;
using clypse.core.Cloud.Interfaces;
using Moq;

namespace clypse.core.UnitTests.Vault;

public class InvalidTestAwsCloudStorageProvider : ICloudStorageProvider
{
    private readonly Mock<ICloudStorageProvider> mockCloudStorageProvider;

    public InvalidTestAwsCloudStorageProvider(Mock<ICloudStorageProvider> mockCloudStorageProvider)
    {
        this.mockCloudStorageProvider = mockCloudStorageProvider;
    }

    public Task<bool> DeleteObjectAsync(
        string key,
        CancellationToken cancellationToken)
    {
        return this.mockCloudStorageProvider.Object.DeleteObjectAsync(
            key,
            cancellationToken);
    }

    public Task<Stream?> GetObjectAsync(
        string key,
        CancellationToken cancellationToken)
    {
        return this.mockCloudStorageProvider.Object.GetObjectAsync(
            key,
            cancellationToken);
    }

    public Task<List<string>> ListObjectsAsync(
        string prefix,
        string? delimiter,
        CancellationToken cancellationToken)
    {
        return this.mockCloudStorageProvider.Object.ListObjectsAsync(
            prefix,
            delimiter,
            cancellationToken);
    }

    public Task<bool> PutObjectAsync(
        string key,
        Stream data,
        MetadataCollection? metaData,
        CancellationToken cancellationToken)
    {
        return this.mockCloudStorageProvider.Object.PutObjectAsync(
            key,
            data,
            metaData,
            cancellationToken);
    }
}
