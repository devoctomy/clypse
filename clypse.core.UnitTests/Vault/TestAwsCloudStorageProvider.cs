using Amazon.S3.Model;
using clypse.core.Cloud;
using clypse.core.Cloud.Interfaces;
using clypse.core.Cryptography.Interfaces;
using Moq;

namespace clypse.core.UnitTests.Vault;

public class TestAwsCloudStorageProvider : ICloudStorageProvider, IAwsEncryptedCloudStorageProviderTransformer
{
    private readonly Mock<ICloudStorageProvider> mockCloudStorageProvider;
    private readonly Mock<IAwsEncryptedCloudStorageProviderTransformer> awsEncryptedCloudStorageProviderTransformer;

    public TestAwsCloudStorageProvider(
        Mock<ICloudStorageProvider> mockCloudStorageProvider,
        Mock<IAwsEncryptedCloudStorageProviderTransformer> awsEncryptedCloudStorageProviderTransformer)
    {
        this.mockCloudStorageProvider = mockCloudStorageProvider;
        this.awsEncryptedCloudStorageProviderTransformer = awsEncryptedCloudStorageProviderTransformer;
    }

    public AwsS3E2eCloudStorageProvider CreateE2eProvider(ICryptoService cryptoService)
    {
        return this.awsEncryptedCloudStorageProviderTransformer.Object.CreateE2eProvider(cryptoService);
    }

    public AwsS3SseCCloudStorageProvider CreateSseProvider()
    {
        return this.awsEncryptedCloudStorageProviderTransformer.Object.CreateSseProvider();
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
