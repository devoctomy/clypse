using Amazon.S3;
using Amazon.S3.Model;
using clypse.core.Cloud;
using clypse.core.Cloud.Aws.S3;
using clypse.core.Cloud.Exceptions;
using Moq;

namespace clypse.core.UnitTests.Cloud;

public class AwsCloudStorageProviderBaseTests
{
    [Fact]
    public async Task GivenBucketName_AndKey_WhenDeleteObjectAsync_ThenGetObjectMetadata_AndDeleteObjectAsync_AndReturnTrue()
    {
        // Arrange
        var bucketName = "Foo";
        var mockAmazonS3Client = new Mock<IAmazonS3Client>();
        var sut = new AwsCloudStorageProviderBase(bucketName, mockAmazonS3Client.Object);

        var key = "Bar";
        var getObjectMetadataRequest = default(GetObjectMetadataRequest);
        var getObjectMetaDataResponse = new GetObjectMetadataResponse();
        var deleteObjectRequest = default(DeleteObjectRequest);
        var deleteObjectResponse = new DeleteObjectResponse();
        var cancellationTokenSource = new CancellationTokenSource();

        mockAmazonS3Client.Setup(x => x.GetObjectMetadataAsync(
            It.Is<GetObjectMetadataRequest>(y => y == getObjectMetadataRequest),
            It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)))
            .ReturnsAsync(getObjectMetaDataResponse);

        mockAmazonS3Client.Setup(x => x.DeleteObjectAsync(
            It.Is<DeleteObjectRequest>(y => y == deleteObjectRequest),
            It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)))
            .ReturnsAsync(deleteObjectResponse);

        // Act
        var result = await sut.DeleteObjectAsync(
            key,
            cancellationTokenSource.Token);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task GivenBucketName_AndKey_AndObjectNotExists_WhenDeleteObjectAsync_ThenGetObjectMetadataFailed_AndReturnFalse()
    {
        // Arrange
        var bucketName = "Foo";
        var mockAmazonS3Client = new Mock<IAmazonS3Client>();
        var sut = new AwsCloudStorageProviderBase(bucketName, mockAmazonS3Client.Object);

        var key = "Bar";
        var deleteObjectRequest = default(DeleteObjectRequest);
        var deleteObjectResponse = new DeleteObjectResponse();
        var cancellationTokenSource = new CancellationTokenSource();

        mockAmazonS3Client.Setup(x => x.GetObjectMetadataAsync(
            It.IsAny<GetObjectMetadataRequest>(),
            It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)))
            .Callback(() =>
            {
                throw new AmazonS3Exception("Something went wrong!")
                {
                    StatusCode = System.Net.HttpStatusCode.NotFound,
                };
            });

        mockAmazonS3Client.Setup(x => x.DeleteObjectAsync(
            It.Is<DeleteObjectRequest>(y => y == deleteObjectRequest),
            It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)))
            .ReturnsAsync(deleteObjectResponse);

        // Act
        var result = await sut.DeleteObjectAsync(
            key,
            cancellationTokenSource.Token);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GivenBucketName_AndKey_WhenDeleteObjectAsync_ThenGetObjectMetadata_AndDeleteObjectAsyncFailed_AndCloudStorageProviderExceptionThrown()
    {
        // Arrange
        var bucketName = "Foo";
        var mockAmazonS3Client = new Mock<IAmazonS3Client>();
        var sut = new AwsCloudStorageProviderBase(bucketName, mockAmazonS3Client.Object);

        var key = "Bar";
        var getObjectMetaDataResponse = new GetObjectMetadataResponse();
        var deleteObjectResponse = new DeleteObjectResponse();
        var cancellationTokenSource = new CancellationTokenSource();

        mockAmazonS3Client.Setup(x => x.GetObjectMetadataAsync(
            It.IsAny<GetObjectMetadataRequest>(),
            It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)))
            .ReturnsAsync(getObjectMetaDataResponse);

        mockAmazonS3Client.Setup(x => x.DeleteObjectAsync(
            It.IsAny<DeleteObjectRequest>(),
            It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)))
            .Callback(() =>
            {
                throw new AmazonS3Exception("Something went wrong!");
            });

        // Act & Assert
        await Assert.ThrowsAnyAsync<CloudStorageProviderException>(async () =>
        {
            _ = await sut.DeleteObjectAsync(
                key,
                cancellationTokenSource.Token);
        });
    }

    [Fact]
    public async Task GivenBucketName_AndKey_WhenGetObjectAsync_ThenGetObjectAsync_AndObjectReturned()
    {
        // Arrange
        var bucketName = "Foo";
        var mockAmazonS3Client = new Mock<IAmazonS3Client>();
        var sut = new AwsCloudStorageProviderBase(bucketName, mockAmazonS3Client.Object);

        var key = "Bar";
        using var processedResponseStream = new MemoryStream();
        var cancellationTokenSource = new CancellationTokenSource();
        var getObjectResponse = new GetObjectResponse
        {
            ResponseStream = processedResponseStream,
        };

        mockAmazonS3Client.Setup(x => x.GetObjectAsync(
            It.IsAny<GetObjectRequest>(),
            It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)))
            .ReturnsAsync(getObjectResponse);

        // Act
        var result = await sut.GetObjectAsync(
            key,
            cancellationTokenSource.Token);

        // Assert
        Assert.Equal(processedResponseStream, result);
    }

    [Fact]
    public async Task GivenBucketName_AndKey_AndObjectNotExists_WhenGetObjectAsync_ThenGetObjectAsync_AndFalseReturned()
    {
        // Arrange
        var bucketName = "Foo";
        var mockAmazonS3Client = new Mock<IAmazonS3Client>();
        var sut = new AwsCloudStorageProviderBase(bucketName, mockAmazonS3Client.Object);

        var key = "Bar";
        using var processedResponseStream = new MemoryStream();
        var cancellationTokenSource = new CancellationTokenSource();
        var getObjectResponse = new GetObjectResponse();

        mockAmazonS3Client.Setup(x => x.GetObjectAsync(
            It.IsAny<GetObjectRequest>(),
            It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)))
            .Callback(() =>
            {
                throw new AmazonS3Exception("Something went wrong!")
                {
                    StatusCode = System.Net.HttpStatusCode.NotFound,
                };
            });

        // Act
        var result = await sut.GetObjectAsync(
            key,
            cancellationTokenSource.Token);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GivenBucketName_AndKey_WhenGetObjectAsync_ThenGetObjectAsyncFailed_AndCloudStorageProviderExceptionThrown()
    {
        // Arrange
        var bucketName = "Foo";
        var mockAmazonS3Client = new Mock<IAmazonS3Client>();
        var sut = new AwsCloudStorageProviderBase(bucketName, mockAmazonS3Client.Object);

        var key = "Bar";
        using var processedResponseStream = new MemoryStream();
        var cancellationTokenSource = new CancellationTokenSource();
        var getObjectResponse = new GetObjectResponse();

        mockAmazonS3Client.Setup(x => x.GetObjectAsync(
            It.IsAny<GetObjectRequest>(),
            It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)))
            .Callback(() =>
            {
                throw new AmazonS3Exception("Something went wrong!");
            });

        // Act & Assert
        await Assert.ThrowsAnyAsync<CloudStorageProviderException>(async () =>
        {
            _ = await sut.GetObjectAsync(
                key,
                cancellationTokenSource.Token);
        });
    }

    [Fact]
    public async Task GivenBucketName_AndPrefix_WhenListObjectsAsync_ThenListObjectsV2Async_AndCallbackInvoked_AndObjectKeysReturned()
    {
        // Arrange
        var bucketName = "Foo";
        var mockAmazonS3Client = new Mock<IAmazonS3Client>();
        var sut = new AwsCloudStorageProviderBase(bucketName, mockAmazonS3Client.Object);

        var prefix = "Bar/";
        var cancellationTokenSource = new CancellationTokenSource();

        // Setup the mock to return a list of objects
        var listObjectsResponse = new ListObjectsV2Response
        {
            S3Objects =
            [
                new S3Object { Key = "Bar/file1.txt" },
                new S3Object { Key = "Bar/file2.txt" },
                new S3Object { Key = "Bar/subfolder/file3.txt" },
            ],
            IsTruncated = false,
        };

        mockAmazonS3Client.Setup(x => x.ListObjectsV2Async(
            It.IsAny<ListObjectsV2Request>(),
            It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)))
            .ReturnsAsync(listObjectsResponse);

        // Act
        var result = await sut.ListObjectsAsync(
            prefix,
            cancellationTokenSource.Token);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains("Bar/file1.txt", result);
        Assert.Contains("Bar/file2.txt", result);
        Assert.Contains("Bar/subfolder/file3.txt", result);
    }
}
