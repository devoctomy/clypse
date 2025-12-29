using Amazon.CloudFront;
using Amazon.CloudFront.Model;
using Microsoft.Extensions.Logging;

namespace clypse.portal.setup.Services.Cloudfront;

public class CloudfrontService(
    IAmazonCloudFront amazonCloudFront,
    ILogger<CloudfrontService> logger) : ICloudfrontService
{
    public async Task<string?> CreateDistributionAsync(
        string websiteHost,
        string? alias = null,
        string? certificateArn = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var originId = $"{websiteHost}-{Guid.NewGuid().ToString()[..8]}";

            var distributionConfig = new DistributionConfig
            {
                CallerReference = Guid.NewGuid().ToString(),
                Enabled = true,
                Origins = new Origins
                {
                    Quantity = 1,
                    Items =
                    [
                        new Origin
                        {
                            Id = originId,
                            DomainName = "localstack", //websiteHost,
                            OriginPath = "/testing.clypse.portal",
                            CustomOriginConfig = new CustomOriginConfig
                            {
                                //HTTPPort = 4566, //80,
                                //HTTPSPort = 4566, //443,
                                OriginProtocolPolicy = OriginProtocolPolicy.HttpOnly
                            }
                        }
                    ]
                },
                DefaultCacheBehavior = new DefaultCacheBehavior
                {
                    TargetOriginId = originId,
                    ViewerProtocolPolicy = ViewerProtocolPolicy.RedirectToHttps,
                    AllowedMethods = new AllowedMethods
                    {
                        Quantity = 2,
                        Items = ["HEAD", "GET"],
                        CachedMethods = new CachedMethods
                        {
                            Quantity = 2,
                            Items = ["HEAD", "GET"]
                        }
                    },
                    Compress = true,
                    CachePolicyId = "658327ea-f89d-4fab-a63d-7e88639e58f6"
                }
            };

            if (!string.IsNullOrWhiteSpace(alias))
            {
                distributionConfig.Aliases = new Aliases
                {
                    Quantity = 1,
                    Items = [alias]
                };
            }

            if (!string.IsNullOrWhiteSpace(certificateArn))
            {
                distributionConfig.ViewerCertificate = new ViewerCertificate
                {
                    ACMCertificateArn = certificateArn,
                    SSLSupportMethod = SSLSupportMethod.SniOnly,
                    MinimumProtocolVersion = MinimumProtocolVersion.TLSv1_2016
                };
            }

            var request = new CreateDistributionRequest
            {
                DistributionConfig = distributionConfig
            };

            var response = await amazonCloudFront.CreateDistributionAsync(request, cancellationToken);
            
            logger.LogInformation(
                "CloudFront distribution created successfully. Distribution ID: {DistributionId}, Domain: {DomainName}",
                response.Distribution.Id,
                response.Distribution.DomainName);

            return response.Distribution.DomainName;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create CloudFront distribution for {WebsiteHost}", websiteHost);
            return null;
        }
    }
}
