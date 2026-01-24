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
    public async Task GivenWebsiteHostAndCertificateArn_WhenCreateDistribution_ThenCreatesDistributionWithCertificate()
    {
        // Arrange
        var mockAmazonCloudFront = new Mock<IAmazonCloudFront>();
        var mockLogger = new Mock<ILogger<CloudfrontService>>();
        var sut = new CloudfrontService(
            mockAmazonCloudFront.Object,
            mockLogger.Object);
        var websiteHost = "example.s3.amazonaws.com";
        var certificateArn = "arn:aws:acm:us-east-1:123456789012:certificate/12345678-1234-1234-1234-123456789012";
        var expectedDomainName = "d1234567890abc.cloudfront.net";

        mockAmazonCloudFront
            .Setup(cf => cf.CreateDistributionAsync(
                It.Is<CreateDistributionRequest>(req =>
                    req.DistributionConfig.Origins.Items[0].DomainName == websiteHost &&
                    req.DistributionConfig.ViewerCertificate != null &&
                    req.DistributionConfig.ViewerCertificate.ACMCertificateArn == certificateArn &&
                    req.DistributionConfig.ViewerCertificate.SSLSupportMethod == SSLSupportMethod.SniOnly &&
                    req.DistributionConfig.ViewerCertificate.MinimumProtocolVersion == MinimumProtocolVersion.TLSv1_2016),
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
        var domainName = await sut.CreateDistributionAsync(websiteHost, certificateArn: certificateArn);

        // Assert
        Assert.Equal(expectedDomainName, domainName);
        mockAmazonCloudFront.Verify(cf => cf.CreateDistributionAsync(
            It.Is<CreateDistributionRequest>(req =>
                req.DistributionConfig.ViewerCertificate != null &&
                req.DistributionConfig.ViewerCertificate.ACMCertificateArn == certificateArn),
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
    public async Task GivenEmptyCertificateArn_WhenCreateDistribution_ThenDoesNotSetCertificate()
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
                    req.DistributionConfig.ViewerCertificate == null),
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
        var domainName = await sut.CreateDistributionAsync(websiteHost, certificateArn: string.Empty);

        // Assert
        Assert.Equal(expectedDomainName, domainName);
        mockAmazonCloudFront.Verify(cf => cf.CreateDistributionAsync(
            It.Is<CreateDistributionRequest>(req =>
                req.DistributionConfig.ViewerCertificate == null),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
