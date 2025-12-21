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

    [Fact]
    public async Task GivenCorsConfiguration_WhenSetBucketCorsConfigurationAsync_ThenSetsCorsConfiguration()
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
        var allowedHeaders = new List<string> { "*" };
        var allowedMethods = new List<string> { "GET", "POST", "PUT", "DELETE", "HEAD" };
        var allowedOrigins = new List<string> { "https://example.com" };

        mockAmazonS3
            .Setup(s3 => s3.PutCORSConfigurationAsync(
                expectedBucketName,
                It.Is<CORSConfiguration>(config =>
                    config.Rules.Count == 1 &&
                    config.Rules[0].AllowedHeaders.SequenceEqual(allowedHeaders) &&
                    config.Rules[0].AllowedMethods.SequenceEqual(allowedMethods) &&
                    config.Rules[0].AllowedOrigins.SequenceEqual(allowedOrigins) &&
                    config.Rules[0].ExposeHeaders.SequenceEqual(new[] { "ETag" }) &&
                    config.Rules[0].MaxAgeSeconds == 3000),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PutCORSConfigurationResponse
            {
                HttpStatusCode = System.Net.HttpStatusCode.OK
            });

        // Act
        var success = await s3Service.SetBucketCorsConfigurationAsync(
            bucketName,
            allowedHeaders,
            allowedMethods,
            allowedOrigins);

        // Assert
        Assert.True(success);
        mockAmazonS3.Verify(s3 => s3.PutCORSConfigurationAsync(
            expectedBucketName,
            It.IsAny<CORSConfiguration>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GivenCorsConfiguration_WhenSetBucketCorsConfigurationAsyncFails_ThenReturnsFalse()
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
        var allowedHeaders = new List<string> { "*" };
        var allowedMethods = new List<string> { "GET" };
        var allowedOrigins = new List<string> { "*" };

        mockAmazonS3
            .Setup(s3 => s3.PutCORSConfigurationAsync(
                It.IsAny<string>(),
                It.IsAny<CORSConfiguration>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PutCORSConfigurationResponse
            {
                HttpStatusCode = System.Net.HttpStatusCode.BadRequest
            });

        // Act
        var success = await s3Service.SetBucketCorsConfigurationAsync(
            bucketName,
            allowedHeaders,
            allowedMethods,
            allowedOrigins);

        // Assert
        Assert.False(success);
    }

    [Fact]
    public async Task GivenBucketPolicy_WhenSetBucketPolicyAsync_ThenSetsBucketPolicy()
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
        var policyDocument = new
        {
            Version = "2012-10-17",
            Statement = new[]
            {
                new
                {
                    Effect = "Allow",
                    Principal = "*",
                    Action = "s3:GetObject",
                    Resource = $"arn:aws:s3:::{expectedBucketName}/*"
                }
            }
        };

        mockAmazonS3
            .Setup(s3 => s3.PutBucketPolicyAsync(
                It.Is<PutBucketPolicyRequest>(req =>
                    req.BucketName == expectedBucketName &&
                    req.Policy.Contains("2012-10-17") &&
                    req.Policy.Contains("s3:GetObject")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PutBucketPolicyResponse
            {
                HttpStatusCode = System.Net.HttpStatusCode.OK
            });

        // Act
        var success = await s3Service.SetBucketPolicyAsync(
            bucketName,
            policyDocument);

        // Assert
        Assert.True(success);
        mockAmazonS3.Verify(s3 => s3.PutBucketPolicyAsync(
            It.Is<PutBucketPolicyRequest>(req => req.BucketName == expectedBucketName),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GivenBucketPolicy_WhenSetBucketPolicyAsyncFails_ThenReturnsFalse()
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
        var policyDocument = new { Version = "2012-10-17" };

        mockAmazonS3
            .Setup(s3 => s3.PutBucketPolicyAsync(
                It.IsAny<PutBucketPolicyRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PutBucketPolicyResponse
            {
                HttpStatusCode = System.Net.HttpStatusCode.Forbidden
            });

        // Act
        var success = await s3Service.SetBucketPolicyAsync(
            bucketName,
            policyDocument);

        // Assert
        Assert.False(success);
    }

    [Fact]
    public async Task GivenWebsiteConfiguration_WhenSetBucketWebsiteConfigurationAsync_ThenSetsWebsiteConfiguration()
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
        var indexDocumentSuffix = "index.html";
        var errorDocument = "error.html";

        mockAmazonS3
            .Setup(s3 => s3.PutBucketWebsiteAsync(
                It.Is<PutBucketWebsiteRequest>(req =>
                    req.BucketName == expectedBucketName &&
                    req.WebsiteConfiguration.IndexDocumentSuffix == indexDocumentSuffix &&
                    req.WebsiteConfiguration.ErrorDocument == errorDocument),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PutBucketWebsiteResponse
            {
                HttpStatusCode = System.Net.HttpStatusCode.OK
            });

        // Act
        var success = await s3Service.SetBucketWebsiteConfigurationAsync(
            bucketName,
            indexDocumentSuffix,
            errorDocument);

        // Assert
        Assert.True(success);
        mockAmazonS3.Verify(s3 => s3.PutBucketWebsiteAsync(
            It.Is<PutBucketWebsiteRequest>(req => req.BucketName == expectedBucketName),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GivenWebsiteConfiguration_WhenSetBucketWebsiteConfigurationAsyncFails_ThenReturnsFalse()
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

        mockAmazonS3
            .Setup(s3 => s3.PutBucketWebsiteAsync(
                It.IsAny<PutBucketWebsiteRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PutBucketWebsiteResponse
            {
                HttpStatusCode = System.Net.HttpStatusCode.Forbidden
            });

        // Act
        var success = await s3Service.SetBucketWebsiteConfigurationAsync(bucketName);

        // Assert
        Assert.False(success);
    }
}
