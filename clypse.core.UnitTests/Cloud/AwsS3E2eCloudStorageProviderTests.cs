using System.Text;
using Amazon.S3;
using Amazon.S3.Model;
using clypse.core.Cloud;
using clypse.core.Cloud.Aws.S3;
using clypse.core.Cryptogtaphy;
using clypse.core.Cryptogtaphy.Interfaces;
using clypse.core.UnitTests.Extensions;
using Moq;

namespace clypse.core.UnitTests.Cloud;

public class AwsS3E2eCloudStorageProviderTests
{
    [Fact]
    public async Task GivenBucketName_AndCredentials_AndEncryptionKey_AndData_WhenPutObjectAsync_AndGetObjectAsync_AndDeleteObjectAsync_ThenObjectSuccessfullyPut_AndObjectSuccessfullyGot_AndObjectSuccessfullyDeleted()
    {
        // Arrange
        var bucketName = "Foo";
        var mockAmazonS3Client = new Mock<IAmazonS3Client>();
        var mockCryptoService = new Mock<ICryptoService>();
        using var randomGeneratorService = new RandomGeneratorService();
        var sut = new AwsS3E2eCloudStorageProvider(bucketName, mockAmazonS3Client.Object, mockCryptoService.Object);

        var key = Guid.NewGuid().ToString();
        var plainText = "The quick brown fox jumps over the lazy dog!";
        var data = Encoding.UTF8.GetBytes(plainText);
        using var dataStream = new MemoryStream(data);
        using var getObjectResponseStream = new MemoryStream(data);
        var encryptionKey = randomGeneratorService.GetRandomBytes(32);

        mockAmazonS3Client.Setup(x => x.PutObjectAsync(
            It.IsAny<PutObjectRequest>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PutObjectResponse());

        mockAmazonS3Client.Setup(x => x.GetObjectAsync(
            It.IsAny<GetObjectRequest>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetObjectResponse
            {
                ResponseStream = getObjectResponseStream,
            });

        mockCryptoService.Setup(x => x.EncryptAsync(
            It.IsAny<Stream>(),
            It.IsAny<Stream>(),
            It.IsAny<string>()))
            .Callback(async (Stream i, Stream o, string key) =>
            {
                await o.WriteAsync(data);
            });

        mockCryptoService.Setup(x => x.DecryptAsync(
            It.IsAny<Stream>(),
            It.IsAny<Stream>(),
            It.IsAny<string>()))
            .Callback(async (Stream i, Stream o, string key) =>
            {
                await o.WriteAsync(data);
            });

        // Act
        var retrievedData = (byte[]?)null;
        var put = await sut.PutEncryptedObjectAsync(
            key,
            dataStream,
            Convert.ToBase64String(encryptionKey),
            null,
            CancellationToken.None);
        var deleted = false;
        if (put)
        {
            using var retrievedDataStream = await sut.GetEncryptedObjectAsync(key, Convert.ToBase64String(encryptionKey), CancellationToken.None);
            retrievedData = new byte[retrievedDataStream!.Length];
            await retrievedDataStream.ReadAllAsync(retrievedData, CancellationToken.None);
            deleted = await sut.DeleteEncryptedObjectAsync(key, Convert.ToBase64String(encryptionKey), CancellationToken.None);
        }

        // Assert
        Assert.True(put);
        Assert.NotNull(retrievedData);
        Assert.True(deleted);
        Assert.Equal(plainText, Encoding.UTF8.GetString(retrievedData));

        mockAmazonS3Client.Verify(
            x => x.PutObjectAsync(
            It.IsAny<PutObjectRequest>(),
            It.IsAny<CancellationToken>()), Times.Once);
        mockAmazonS3Client.Verify(
            x => x.GetObjectAsync(
            It.IsAny<GetObjectRequest>(),
            It.IsAny<CancellationToken>()), Times.Once);
        mockCryptoService.Verify(
            x => x.EncryptAsync(
            It.IsAny<Stream>(),
            It.IsAny<Stream>(),
            It.Is<string>(y => y == Convert.ToBase64String(encryptionKey))), Times.Once);
        mockCryptoService.Verify(
            x => x.DecryptAsync(
            It.IsAny<Stream>(),
            It.IsAny<Stream>(),
            It.Is<string>(y => y == Convert.ToBase64String(encryptionKey))), Times.Once);
        mockAmazonS3Client.Verify(
            x => x.DeleteObjectAsync(
            It.IsAny<DeleteObjectRequest>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenBucketName_AndCredentials_AndEncryptionKey_AndWrongDecryptionKey_AndData_WhenPutObjectAsync_AndGetObjectAsync_AndDeleteObjectAsync_ThenObjectSuccessfullyPut_AndObjectSuccessfullyGot_AndObjectSuccessfullyDeleted()
    {
        // Arrange
        var bucketName = "Foo";
        var mockAmazonS3Client = new Mock<IAmazonS3Client>();
        var mockCryptoService = new Mock<ICryptoService>();
        using var randomGeneratorService = new RandomGeneratorService();
        var sut = new AwsS3E2eCloudStorageProvider(bucketName, mockAmazonS3Client.Object, mockCryptoService.Object);

        var key = Guid.NewGuid().ToString();
        var plainText = "The quick brown fox jumps over the lazy dog!";
        var data = Encoding.UTF8.GetBytes(plainText);
        using var dataStream = new MemoryStream(data);
        using var getObjectResponseStream = new MemoryStream(data);
        var encryptionKey = randomGeneratorService.GetRandomBytes(32);
        var decyptionKey = encryptionKey;
        decyptionKey[0] = 69;

        mockAmazonS3Client.Setup(
            x => x.PutObjectAsync(
            It.IsAny<PutObjectRequest>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PutObjectResponse());

        mockAmazonS3Client.Setup(
            x => x.GetObjectAsync(
            It.IsAny<GetObjectRequest>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetObjectResponse
            {
                ResponseStream = getObjectResponseStream,
            });

        mockCryptoService.Setup(
            x => x.DecryptAsync(
            It.IsAny<Stream>(),
            It.IsAny<Stream>(),
            It.IsAny<string>()))
            .Callback(async (Stream i, Stream o, string key) =>
            {
                await Task.Yield();
                throw new Exception();
            });

        // Act & Assert
        var retrievedData = (byte[]?)null;
        var put = await sut.PutEncryptedObjectAsync(
            key,
            dataStream,
            Convert.ToBase64String(encryptionKey),
            null,
            CancellationToken.None);
        var deleted = false;
        if (put)
        {
            var retrievedDataStream = await sut.GetEncryptedObjectAsync(key, Convert.ToBase64String(decyptionKey), CancellationToken.None);
            retrievedData = new byte[retrievedDataStream!.Length];
            await retrievedDataStream.ReadAllAsync(retrievedData, CancellationToken.None);
            deleted = await sut.DeleteEncryptedObjectAsync(key, Convert.ToBase64String(encryptionKey), CancellationToken.None);
        }

        Assert.True(put);
        Assert.NotNull(retrievedData);
        Assert.True(deleted);

        mockAmazonS3Client.Verify(
            x => x.PutObjectAsync(
            It.IsAny<PutObjectRequest>(),
            It.IsAny<CancellationToken>()), Times.Once);
        mockAmazonS3Client.Verify(
            x => x.GetObjectAsync(
            It.IsAny<GetObjectRequest>(),
            It.IsAny<CancellationToken>()), Times.Once);
        mockCryptoService.Verify(
            x => x.DecryptAsync(
            It.IsAny<Stream>(),
            It.IsAny<Stream>(),
            It.IsAny<string>()), Times.Once);
        mockAmazonS3Client.Verify(
            x => x.DeleteObjectAsync(
            It.IsAny<DeleteObjectRequest>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenBucketName_AndCredentials_AndEncryptionKey_AndUnknownKey_WhenGetObjectAsync_ThenExceptionThrown()
    {
        // Arrange
        var bucketName = "Foo";
        var mockAmazonS3Client = new Mock<IAmazonS3Client>();
        var mockCryptoService = new Mock<ICryptoService>();
        var sut = new AwsS3E2eCloudStorageProvider(bucketName, mockAmazonS3Client.Object, mockCryptoService.Object);

        var key = Guid.NewGuid().ToString();
        var encryptionKey = new byte[32];

        mockAmazonS3Client.Setup(x => x.GetObjectAsync(
            It.IsAny<GetObjectRequest>(),
            It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                throw new AmazonS3Exception("Foobar")
                {
                    StatusCode = System.Net.HttpStatusCode.NotFound,
                };
            });

        mockAmazonS3Client.Setup(x => x.GetObjectMetadataAsync(
            It.IsAny<GetObjectMetadataRequest>(),
            It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                throw new AmazonS3Exception("Foobar")
                {
                    StatusCode = System.Net.HttpStatusCode.NotFound,
                };
            });

        // Act
        var retrievedData = await sut.GetEncryptedObjectAsync(key, Convert.ToBase64String(encryptionKey), CancellationToken.None);
        var deleted = await sut.DeleteEncryptedObjectAsync(key, Convert.ToBase64String(encryptionKey), CancellationToken.None);

        // Assert
        Assert.Null(retrievedData);
        Assert.False(deleted);

        mockAmazonS3Client.Verify(
            x => x.GetObjectAsync(
            It.IsAny<GetObjectRequest>(),
            It.IsAny<CancellationToken>()), Times.Once);
        mockAmazonS3Client.Verify(
            x => x.GetObjectMetadataAsync(
            It.IsAny<GetObjectMetadataRequest>(),
            It.IsAny<CancellationToken>()), Times.Once);
        mockAmazonS3Client.Verify(
            x => x.DeleteObjectAsync(
            It.IsAny<DeleteObjectRequest>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}
