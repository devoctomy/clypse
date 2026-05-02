using Amazon.CloudFront;
using Amazon.CloudFront.Model;
using clypse.portal.setup.Services.Cloudfront;
using Microsoft.Extensions.Logging;
using Moq;

namespace clypse.portal.setup.UnitTests.Services.Cloudfront;

public class CloudfrontServiceTests
{
    [Fact]
    public async Task GivenWebsiteHost_WhenCreateDistribution_ThenCreatesDistribution()
    {
        // Arrange
        var mockAmazonCloudFront = new Mock<IAmazonCloudFront>();
        var mockLogger = new Mock<ILogger<CloudfrontService>>();
        var sut = new CloudfrontService(
            mockAmazonCloudFront.Object,
            mockLogger.Object);
        var websiteHost = "example.s3.amazonaws.com";
        var expectedDistributionId = "E1234567890ABC";
        var expectedDomainName = "d1234567890abc.cloudfront.net";

        mockAmazonCloudFront
            .Setup(cf => cf.CreateDistributionAsync(
                It.Is<CreateDistributionRequest>(req =>
                    req.DistributionConfig.Origins.Items[0].DomainName == websiteHost &&
                    req.DistributionConfig.Enabled.GetValueOrDefault() &&
                    req.DistributionConfig.DefaultCacheBehavior.ViewerProtocolPolicy == ViewerProtocolPolicy.RedirectToHttps),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateDistributionResponse
            {
                Distribution = new Distribution
                {
                    Id = expectedDistributionId,
                    DomainName = expectedDomainName
                }
            });
        
        // Act
        var domainName = await sut.CreateDistributionAsync(websiteHost);

        // Assert
        Assert.Equal(expectedDomainName, domainName);
        mockAmazonCloudFront.Verify(cf => cf.CreateDistributionAsync(
            It.Is<CreateDistributionRequest>(req =>
                req.DistributionConfig.Origins.Items[0].DomainName == websiteHost &&
                req.DistributionConfig.Enabled.GetValueOrDefault()),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GivenWebsiteHostAndAlias_WhenCreateDistribution_ThenCreatesDistributionWithAlias()
    {
        // Arrange
        var mockAmazonCloudFront = new Mock<IAmazonCloudFront>();
        var mockLogger = new Mock<ILogger<CloudfrontService>>();
        var sut = new CloudfrontService(
            mockAmazonCloudFront.Object,
            mockLogger.Object);
        var websiteHost = "example.s3.amazonaws.com";
        var alias = "www.example.com";
        var expectedDomainName = "d1234567890abc.cloudfront.net";

        mockAmazonCloudFront
            .Setup(cf => cf.CreateDistributionAsync(
                It.Is<CreateDistributionRequest>(req =>
                    req.DistributionConfig.Origins.Items[0].DomainName == websiteHost &&
                    req.DistributionConfig.Aliases != null &&
                    req.DistributionConfig.Aliases.Quantity == 1 &&
                    req.DistributionConfig.Aliases.Items.Contains(alias)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateDistributionResponse
            {
                Distribution = new Distribution
                {
                    Id = "E1234567890ABC",
                    DomainName = expectedDomainName
                }
            });
        
        // Act
        var domainName = await sut.CreateDistributionAsync(websiteHost, alias);

        // Assert
        Assert.Equal(expectedDomainName, domainName);
        mockAmazonCloudFront.Verify(cf => cf.CreateDistributionAsync(
            It.Is<CreateDistributionRequest>(req =>
                req.DistributionConfig.Aliases != null &&
                req.DistributionConfig.Aliases.Items.Contains(alias)),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GivenWebsiteHostAliasAndCertificateArn_WhenCreateDistribution_ThenCreatesDistributionWithAliasAndCertificate()
    {
        // Arrange
        var mockAmazonCloudFront = new Mock<IAmazonCloudFront>();
        var mockLogger = new Mock<ILogger<CloudfrontService>>();
        var sut = new CloudfrontService(
            mockAmazonCloudFront.Object,
            mockLogger.Object);
        var websiteHost = "example.s3.amazonaws.com";
        var alias = "www.example.com";
        var certificateArn = "arn:aws:acm:us-east-1:123456789012:certificate/12345678-1234-1234-1234-123456789012";
        var expectedDomainName = "d1234567890abc.cloudfront.net";

        mockAmazonCloudFront
            .Setup(cf => cf.CreateDistributionAsync(
                It.Is<CreateDistributionRequest>(req =>
                    req.DistributionConfig.Origins.Items[0].DomainName == websiteHost &&
                    req.DistributionConfig.Aliases != null &&
                    req.DistributionConfig.Aliases.Items.Contains(alias) &&
                    req.DistributionConfig.ViewerCertificate != null &&
                    req.DistributionConfig.ViewerCertificate.ACMCertificateArn == certificateArn),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateDistributionResponse
            {
                Distribution = new Distribution
                {
                    Id = "E1234567890ABC",
                    DomainName = expectedDomainName
                }
            });
        
        // Act
        var domainName = await sut.CreateDistributionAsync(websiteHost, alias, certificateArn);

        // Assert
        Assert.Equal(expectedDomainName, domainName);
        mockAmazonCloudFront.Verify(cf => cf.CreateDistributionAsync(
            It.Is<CreateDistributionRequest>(req =>
                req.DistributionConfig.Aliases != null &&
                req.DistributionConfig.Aliases.Items.Contains(alias) &&
                req.DistributionConfig.ViewerCertificate != null &&
                req.DistributionConfig.ViewerCertificate.ACMCertificateArn == certificateArn),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GivenException_WhenCreateDistribution_ThenReturnsNull()
    {
        // Arrange
        var mockAmazonCloudFront = new Mock<IAmazonCloudFront>();
        var mockLogger = new Mock<ILogger<CloudfrontService>>();
        var sut = new CloudfrontService(
            mockAmazonCloudFront.Object,
            mockLogger.Object);
        var websiteHost = "example.s3.amazonaws.com";

        mockAmazonCloudFront
            .Setup(cf => cf.CreateDistributionAsync(
                It.IsAny<CreateDistributionRequest>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonCloudFrontException("Test exception"));
        
        // Act
        var domainName = await sut.CreateDistributionAsync(websiteHost);

        // Assert
        Assert.Null(domainName);
        mockAmazonCloudFront.Verify(cf => cf.CreateDistributionAsync(
            It.IsAny<CreateDistributionRequest>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GivenWebsiteHost_WhenCreateDistribution_ThenSetsCorrectOriginConfiguration()
    {
        // Arrange
        var mockAmazonCloudFront = new Mock<IAmazonCloudFront>();
        var mockLogger = new Mock<ILogger<CloudfrontService>>();
        var sut = new CloudfrontService(
            mockAmazonCloudFront.Object,
            mockLogger.Object);
        var websiteHost = "example.s3.amazonaws.com";
        var expectedDomainName = "d1234567890abc.cloudfront.net";

        mockAmazonCloudFront
            .Setup(cf => cf.CreateDistributionAsync(
                It.Is<CreateDistributionRequest>(req =>
                    req.DistributionConfig.Origins.Quantity == 1 &&
                    req.DistributionConfig.Origins.Items[0].DomainName == websiteHost &&
                    req.DistributionConfig.Origins.Items[0].CustomOriginConfig != null &&
                    req.DistributionConfig.Origins.Items[0].CustomOriginConfig.HTTPPort == 80 &&
                    req.DistributionConfig.Origins.Items[0].CustomOriginConfig.HTTPSPort == 443 &&
                    req.DistributionConfig.Origins.Items[0].CustomOriginConfig.OriginProtocolPolicy == OriginProtocolPolicy.HttpOnly),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateDistributionResponse
            {
                Distribution = new Distribution
                {
                    Id = "E1234567890ABC",
                    DomainName = expectedDomainName
                }
            });
        
        // Act
        var domainName = await sut.CreateDistributionAsync(websiteHost);

        // Assert
        Assert.Equal(expectedDomainName, domainName);
        mockAmazonCloudFront.Verify(cf => cf.CreateDistributionAsync(
            It.Is<CreateDistributionRequest>(req =>
                req.DistributionConfig.Origins.Items[0].CustomOriginConfig.HTTPPort == 80 &&
                req.DistributionConfig.Origins.Items[0].CustomOriginConfig.HTTPSPort == 443 &&
                req.DistributionConfig.Origins.Items[0].CustomOriginConfig.OriginProtocolPolicy == OriginProtocolPolicy.HttpOnly),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GivenWebsiteHost_WhenCreateDistribution_ThenSetsCorrectCacheBehavior()
    {
        // Arrange
        var mockAmazonCloudFront = new Mock<IAmazonCloudFront>();
        var mockLogger = new Mock<ILogger<CloudfrontService>>();
        var sut = new CloudfrontService(
            mockAmazonCloudFront.Object,
            mockLogger.Object);
        var websiteHost = "example.s3.amazonaws.com";
        var expectedDomainName = "d1234567890abc.cloudfront.net";
        var expectedCachePolicyId = "658327ea-f89d-4fab-a63d-7e88639e58f6";

        mockAmazonCloudFront
            .Setup(cf => cf.CreateDistributionAsync(
                It.Is<CreateDistributionRequest>(req =>
                    req.DistributionConfig.DefaultCacheBehavior.ViewerProtocolPolicy == ViewerProtocolPolicy.RedirectToHttps &&
                    req.DistributionConfig.DefaultCacheBehavior.AllowedMethods.Quantity == 2 &&
                    req.DistributionConfig.DefaultCacheBehavior.AllowedMethods.Items.Contains("HEAD") &&
                    req.DistributionConfig.DefaultCacheBehavior.AllowedMethods.Items.Contains("GET") &&
                    req.DistributionConfig.DefaultCacheBehavior.AllowedMethods.CachedMethods.Quantity == 2 &&
                    req.DistributionConfig.DefaultCacheBehavior.Compress.GetValueOrDefault() &&
                    req.DistributionConfig.DefaultCacheBehavior.CachePolicyId == expectedCachePolicyId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateDistributionResponse
            {
                Distribution = new Distribution
                {
                    Id = "E1234567890ABC",
                    DomainName = expectedDomainName
                }
            });
        
        // Act
        var domainName = await sut.CreateDistributionAsync(websiteHost);

        // Assert
        Assert.Equal(expectedDomainName, domainName);
        mockAmazonCloudFront.Verify(cf => cf.CreateDistributionAsync(
            It.Is<CreateDistributionRequest>(req =>
                req.DistributionConfig.DefaultCacheBehavior.ViewerProtocolPolicy == ViewerProtocolPolicy.RedirectToHttps &&
                req.DistributionConfig.DefaultCacheBehavior.Compress.GetValueOrDefault() &&
                req.DistributionConfig.DefaultCacheBehavior.CachePolicyId == expectedCachePolicyId),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GivenEmptyAlias_WhenCreateDistribution_ThenDoesNotSetAlias()
    {
        // Arrange
        var mockAmazonCloudFront = new Mock<IAmazonCloudFront>();
        var mockLogger = new Mock<ILogger<CloudfrontService>>();
        var sut = new CloudfrontService(
            mockAmazonCloudFront.Object,
            mockLogger.Object);
        var websiteHost = "example.s3.amazonaws.com";
        var expectedDomainName = "d1234567890abc.cloudfront.net";

        mockAmazonCloudFront
            .Setup(cf => cf.CreateDistributionAsync(
                It.Is<CreateDistributionRequest>(req =>
                    req.DistributionConfig.Aliases == null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateDistributionResponse
            {
                Distribution = new Distribution
                {
                    Id = "E1234567890ABC",
                    DomainName = expectedDomainName
                }
            });
        
        // Act
        var domainName = await sut.CreateDistributionAsync(websiteHost, string.Empty);

        // Assert
        Assert.Equal(expectedDomainName, domainName);
        mockAmazonCloudFront.Verify(cf => cf.CreateDistributionAsync(
            It.Is<CreateDistributionRequest>(req =>
                req.DistributionConfig.Aliases == null),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GivenDistributionId_WhenInvalidateDistribution_ThenCreatesInvalidation()
    {
        // Arrange
        var mockAmazonCloudFront = new Mock<IAmazonCloudFront>();
        var mockLogger = new Mock<ILogger<CloudfrontService>>();
        var sut = new CloudfrontService(
            mockAmazonCloudFront.Object,
            mockLogger.Object);
        var distributionId = "E1234567890ABC";
        var expectedInvalidationId = "I1234567890ABC";

        mockAmazonCloudFront
            .Setup(cf => cf.CreateInvalidationAsync(
                It.Is<CreateInvalidationRequest>(req =>
                    req.DistributionId == distributionId &&
                    req.InvalidationBatch.Paths.Items.Count == 1 &&
                    req.InvalidationBatch.Paths.Items[0] == "/*"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateInvalidationResponse
            {
                Invalidation = new Invalidation
                {
                    Id = expectedInvalidationId,
                    Status = "InProgress"
                }
            });
        
        // Act
        var result = await sut.InvalidateDistributionAsync(distributionId);

        // Assert
        Assert.True(result);
        mockAmazonCloudFront.Verify(cf => cf.CreateInvalidationAsync(
            It.Is<CreateInvalidationRequest>(req =>
                req.DistributionId == distributionId &&
                req.InvalidationBatch.Paths.Items[0] == "/*"),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GivenDistributionIdAndSpecificPaths_WhenInvalidateDistribution_ThenCreatesInvalidationWithPaths()
    {
        // Arrange
        var mockAmazonCloudFront = new Mock<IAmazonCloudFront>();
        var mockLogger = new Mock<ILogger<CloudfrontService>>();
        var sut = new CloudfrontService(
            mockAmazonCloudFront.Object,
            mockLogger.Object);
        var distributionId = "E1234567890ABC";
        var paths = new[] { "/index.html", "/app.js", "/styles.css" };
        var expectedInvalidationId = "I1234567890ABC";

        mockAmazonCloudFront
            .Setup(cf => cf.CreateInvalidationAsync(
                It.Is<CreateInvalidationRequest>(req =>
                    req.DistributionId == distributionId &&
                    req.InvalidationBatch.Paths.Quantity == 3 &&
                    req.InvalidationBatch.Paths.Items.Contains("/index.html") &&
                    req.InvalidationBatch.Paths.Items.Contains("/app.js") &&
                    req.InvalidationBatch.Paths.Items.Contains("/styles.css")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateInvalidationResponse
            {
                Invalidation = new Invalidation
                {
                    Id = expectedInvalidationId,
                    Status = "InProgress"
                }
            });
        
        // Act
        var result = await sut.InvalidateDistributionAsync(distributionId, paths);

        // Assert
        Assert.True(result);
        mockAmazonCloudFront.Verify(cf => cf.CreateInvalidationAsync(
            It.Is<CreateInvalidationRequest>(req =>
                req.DistributionId == distributionId &&
                req.InvalidationBatch.Paths.Items.Contains("/index.html") &&
                req.InvalidationBatch.Paths.Items.Contains("/app.js") &&
                req.InvalidationBatch.Paths.Items.Contains("/styles.css")),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GivenException_WhenInvalidateDistribution_ThenReturnsFalse()
    {
        // Arrange
        var mockAmazonCloudFront = new Mock<IAmazonCloudFront>();
        var mockLogger = new Mock<ILogger<CloudfrontService>>();
        var sut = new CloudfrontService(
            mockAmazonCloudFront.Object,
            mockLogger.Object);
        var distributionId = "E1234567890ABC";

        mockAmazonCloudFront
            .Setup(cf => cf.CreateInvalidationAsync(
                It.IsAny<CreateInvalidationRequest>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonCloudFrontException("Test exception"));
        
        // Act
        var result = await sut.InvalidateDistributionAsync(distributionId);

        // Assert
        Assert.False(result);
        mockAmazonCloudFront.Verify(cf => cf.CreateInvalidationAsync(
            It.IsAny<CreateInvalidationRequest>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
