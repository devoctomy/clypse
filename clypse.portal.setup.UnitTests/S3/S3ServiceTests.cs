using Amazon.S3;
using Amazon.S3.Model;
using clypse.portal.setup.S3;
using Microsoft.Extensions.Logging;
using Moq;

namespace clypse.portal.setup.UnitTests.S3;

public class S3ServiceTests
{
    [Fact]
    public async Task GivenBucketName_WhenCreateBucket_ThenCreatesBucket()
    {
        // Arrange
        var mockAmazonS3 = new Mock<IAmazonS3>();
        var options = new AwsServiceOptions
        {
            ResourcePrefix = "test-prefix"
        };
        var s3Service = new S3Service(
            mockAmazonS3.Object,
            options,
            Mock.Of<ILogger<S3Service>>());
        var bucketName = "my-bucket";
        var expectedBucketName = "test-prefix.my-bucket";

        mockAmazonS3
            .Setup(s3 => s3.PutBucketAsync(
                It.Is<PutBucketRequest>(req => req.BucketName == expectedBucketName),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PutBucketResponse
            {
                HttpStatusCode = System.Net.HttpStatusCode.OK
            });
        
        // Act
        var success = await s3Service.CreateBucketAsync(bucketName);

        // Assert
        Assert.True(success);
        mockAmazonS3.Verify(s3 => s3.PutBucketAsync(
            It.Is<PutBucketRequest>(req => req.BucketName == expectedBucketName),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
