using clypse.portal.setup.Extensions;
using clypse.portal.setup.Services.Build;
using clypse.portal.setup.Services.Cloudfront;
using clypse.portal.setup.Services.Cognito;
using clypse.portal.setup.Services.Iam;
using clypse.portal.setup.Services.S3;
using clypse.portal.setup.Services.Security;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text;

namespace clypse.portal.setup.Services.Orchestration;

internal class ClypseAwsSetupOrchestration(
        SetupOptions options,
        ISecurityTokenService securityTokenService,
        IIamService iamService,
        IS3Service s3Service,
        ICognitoService cognitoService,
        ICloudfrontService cloudfrontService,
        IPortalConfigService portalConfigService,
        ILogger<IamService> logger) : IClypseAwsSetupOrchestration
{
    public async Task<bool> PrepareSetup(CancellationToken cancellationToken)
    {
        logger.LogInformation("Preparing Clypse AWS setup orchestration.");

        logger.LogDebug("Using AWS Base Url: {baseUrl}", options.BaseUrl);
        logger.LogDebug("Using AWS Region: {region}", options.Region);
        logger.LogDebug("Using Resource Prefix: {resourcePrefix}", options.ResourcePrefix);
        logger.LogDebug("Using IAM Access Id: {accessId}", options.AccessId);
        logger.LogDebug("Using IAM Secret Access Key: {secretAccessKey}", options.SecretAccessKey.Redact(3));
        logger.LogDebug("Using Portal Build Output Path: {portalBuildOutputPath}", options.PortalBuildOutputPath);
        logger.LogDebug("Using Initial User Email: {initialUserEmail}", options.InitialUserEmail);

        if (!options.IsValid())
        {
            throw new Exception("Options are not valid.");
        }

        // S3
        logger.LogInformation("Checking S3 resources.");
        var portalBucketName = "clypse.portal";
        logger.LogInformation("Checking to see if portal bucket already exists.");
        var portalBucketExists = await s3Service.DoesBucketExistAsync(
            portalBucketName,
            cancellationToken);
        if (portalBucketExists)
        {
            logger.LogError("S3 bucket for portal already exists.");
            return false;
        }

        var dataBucketName = "clypse.data";
        logger.LogInformation("Checking to see if portal bucket already exists.");
        var dataBucketExists = await s3Service.DoesBucketExistAsync(
            dataBucketName,
            cancellationToken);
        if (dataBucketExists)
        {
            logger.LogError("S3 bucket for data already exists.");
            return false;
        }

        return true;
    }

    public async Task<bool> SetupClypseOnAwsAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting Clypse AWS setup orchestration.");

        logger.LogDebug("Using AWS Base Url: {baseUrl}", options.BaseUrl);
        logger.LogDebug("Using AWS Region: {region}", options.Region);
        logger.LogDebug("Using Resource Prefix: {resourcePrefix}", options.ResourcePrefix);
        logger.LogDebug("Using IAM Access Id: {accessId}", options.AccessId);
        logger.LogDebug("Using IAM Secret Access Key: {secretAccessKey}", options.SecretAccessKey.Redact(3));
        logger.LogDebug("Using Portal Build Output Path: {portalBuildOutputPath}", options.PortalBuildOutputPath);

        if (!options.IsValid())
        {
            throw new Exception("Options are not valid.");
        }

        var setupId = Guid.NewGuid().ToString();
        var tags = new Dictionary<string, string>
            {
                { "clypse:setup-id", setupId },
                { "clypse:setup-version", Assembly.GetExecutingAssembly().GetName().Version!.ToString() }
            };

        var tagsList = new StringBuilder();
        foreach (var tag in tags)
        {
            tagsList.AppendLine($"{tag.Key} = {tag.Value}");
        }
        logger.LogInformation("Using the following tags for all created resources: \r\n{tagsList}", tagsList.ToString().TrimEnd());

        if (options.InteractiveMode)
        {
            logger.LogInformation("Press any key to begin.");
            Console.ReadKey();
        }

        // Account Info
        logger.LogInformation("Getting AWS Account Id.");
        var accountId = await securityTokenService.GetAccountIdAsync(cancellationToken);
        logger.LogInformation("Got AWS Account Id '{accountId}'.", accountId);

        // S3
        logger.LogInformation("Setting up S3 resources.");
        logger.LogInformation("Creating S3 bucket for portal.");
        var portalBucketName = "clypse.portal";
        var createdPortalBucket = await s3Service.CreateBucketAsync(
            portalBucketName,
            cancellationToken);
        if(!createdPortalBucket)
        {
            logger.LogError("Failed to create S3 bucket for portal.");
            throw new Exception("Failed to create S3 bucket for portal.");
        }

        logger.LogInformation("Tagging portal bucket.");
        var taggedPortalBucket = await s3Service.SetBucketTags(
            portalBucketName,
            tags,
            cancellationToken);
        if (!taggedPortalBucket)
        {
            logger.LogError("Failed to set tags for portal bucket.");
            throw new Exception("Failed to set tags for portal bucket.");
        }

        logger.LogInformation("Creating S3 bucket for data.");
        var dataBucketName = "clypse.data";
        var createdDataBucket = await s3Service.CreateBucketAsync(
            dataBucketName,
            cancellationToken);
        if (!createdDataBucket)
        {
            logger.LogError("Failed to create S3 bucket for data.");
            throw new Exception("Failed to create S3 bucket for data.");
        }

        logger.LogInformation("Tagging data bucket.");
        var taggedDataBucket = await s3Service.SetBucketTags(
            dataBucketName,
            tags,
            cancellationToken);
        if (!taggedDataBucket)
        {
            logger.LogError("Failed to set tags for data bucket.");
            throw new Exception("Failed to set tags for data bucket.");
        }

        // IAM
        logger.LogInformation("Setting up IAM resources.");
        var identityFolder = "${cognito-identity.amazonaws.com:sub}/*";
        var dataPolicyDocument = new
        {
            Version = "2012-10-17",
            Statement = new object[]
            {
                new
                {
                    Effect = "Allow",
                    Action = new[]
                    {
                        "s3:GetBucketLocation",
                        "s3:ListBucket"
                    },
                    Resource = $"arn:aws:s3:::{options.ResourcePrefix}.{dataBucketName}",
                    Condition = new
                    {
                        StringLike = new Dictionary<string, object>
                        {
                            {
                                "s3:prefix",
                                new[]
                                {
                                    identityFolder
                                }
                            }
                        }
                    }
                },
                new
                {
                    Effect = "Allow",
                    Action = new[]
                    {
                        "s3:GetObject",
                        "s3:PutObject",
                        "s3:DeleteObject"
                    },
                    Resource = $"arn:aws:s3:::{options.ResourcePrefix}.{dataBucketName}/{identityFolder}/*"
                }
            }
        };

        var authPolicyDocument = new
        {
            Version = "2012-10-17",
            Statement = new object[]
            {
                new
                {
                    Effect = "Allow",
                    Action = new[]
                    {
                        "cognito-identity:GetCredentialsForIdentity",
                    },
                    Resource = "*"
                }
            }
        };

        logger.LogInformation("Creating data policy.");
        var dataPolicyArn = await iamService.CreatePolicyAsync(
            "clypse.data.policy",
            dataPolicyDocument,
            tags,
            cancellationToken);
        logger.LogInformation("Data policy '{dataPolicyArn}' created.", dataPolicyArn);

        logger.LogInformation("Creating auth policy.");
        var authPolicyArn = await iamService.CreatePolicyAsync(
            "clypse.auth.policy",
            authPolicyDocument,
            tags,
            cancellationToken);
        logger.LogInformation("Auth policy '{authPolicyArn}' created.", authPolicyArn);

        // Cognito
        logger.LogInformation("Setting up Cognito resources.");

        logger.LogInformation("Creating user pool.");
        var userPoolId = await cognitoService.CreateUserPoolAsync(
            "clypse.user.pool",
            tags,
            cancellationToken);
        logger.LogInformation("User pool '{userPoolId}' created.", userPoolId);

        logger.LogInformation("Creating identity pool.");
        var identityPoolId = await cognitoService.CreateIdentityPoolAsync(
            "clypse.identity.pool",
            tags,
            cancellationToken);
        logger.LogInformation("Identity pool '{userPoolId}' created.", userPoolId);

        logger.LogInformation("Setting identity pool authenticated role.");
        var setAuthenticatedRole = await cognitoService.SetIdentityPoolAuthenticatedRoleAsync(
            identityPoolId,
            dataPolicyArn,
            cancellationToken);
        if (!setAuthenticatedRole)
        {
            logger.LogError("Failed to set authenticated role for identity pool.");
            throw new Exception("Failed to set authenticated role for identity pool.");
        }

        logger.LogInformation("Creating user pool client.");
        var userPoolClientId = await cognitoService.CreateUserPoolClientAsync(
            accountId,
            "clypse.identity.pool.client",
            userPoolId,
            tags,
            cancellationToken);
        if (string.IsNullOrEmpty(userPoolClientId))
        {
            logger.LogError("Failed to create user pool client.");
            throw new Exception("Failed to create user pool client.");
        }

        if (Directory.Exists(options.PortalBuildOutputPath))
        {
            logger.LogInformation("Removing unwanted settings from build output.");
            var oldSettings = Directory.GetFiles(options.PortalBuildOutputPath, "appsettings*.json");
            foreach (var oldSetting in oldSettings)
            {
                logger.LogInformation("Removing portal setting file '{oldSetting}'.", oldSetting);
                File.Delete(oldSetting);
            }

            logger.LogInformation("Reconfiguring portal.");
            using var configStream = await portalConfigService.ConfigureAsync(
                "Data/appsettings.json",
                dataBucketName,
                options.Region,
                userPoolId,
                userPoolClientId,
                options.Region,
                identityPoolId,
                cancellationToken);

            var configFilePath = Path.Combine(options.PortalBuildOutputPath, "appsettings.json");
            using var outputStream = File.OpenWrite(configFilePath);
            await configStream.CopyToAsync(outputStream, cancellationToken);
            await outputStream.FlushAsync(cancellationToken);
            outputStream.Close();

            logger.LogInformation("Deploying portal to portal bucket.");
            var result = await s3Service.UploadDirectoryToBucket(
                portalBucketName,
                options.PortalBuildOutputPath,
                cancellationToken);
        }
        else
        {
            logger.LogWarning("Skipping portal deployment as build output path '{portalBuildOutputPath}' does not exist.", options.PortalBuildOutputPath);
        }

        logger.LogInformation("Setting portal bucket website configuration.");
        var setBucketWebsiteConfig = await s3Service.SetBucketWebsiteConfigurationAsync(
            portalBucketName,
            cancellationToken: cancellationToken);
        if (!setBucketWebsiteConfig)
        {
            logger.LogError("Failed to set portal bucket website configuration.");
            throw new Exception("Failed to set portal bucket website configuration.");
        }

        var portalBucketUrl = GetBucketUrl(portalBucketName);
        logger.LogInformation("Portal Bucket Url : {bucketUrl}", portalBucketUrl);

        var portalWebsiteUrl = GetPortalWebsiteConfigUrl(portalBucketName);
        logger.LogInformation("Portal Website Url : {portalWebsiteUrl}", portalWebsiteUrl);

        logger.LogInformation("Creating CloudFront distribution.");
        var distributionDomain = await cloudfrontService.CreateDistributionAsync(
            portalBucketUrl,
            cancellationToken: cancellationToken);
        if(string.IsNullOrEmpty(distributionDomain))
        {
            logger.LogError("Failed to create CloudFront distribution.");
            throw new Exception("Failed to create CloudFront distribution.");
        }

        var origins = (string[])["http://localhost:8080", GetDistributionOrigin(distributionDomain)];

        if (IsLocalstack())
        {
            logger.LogInformation("Setting portal bucket CORS configuration with origins ({origins}).", string.Join(',', origins));
            var setBucketCorsConfig = await s3Service.SetBucketCorsConfigurationAsync(
                portalBucketName,
                ["*"],
                ["GET", "PUT", "POST", "DELETE", "HEAD"],
                [.. origins],
                cancellationToken);
            if (!setBucketCorsConfig)
            {
                logger.LogError("Failed to set portal bucket CORS configuration.");
                throw new Exception("Failed to set portal bucket CORS configuration.");
            }
        }

        logger.LogInformation("Setting data bucket CORS configuration with origins ({origins}).", string.Join(',', origins));
        var setDataBucketCorsConfig = await s3Service.SetBucketCorsConfigurationAsync(
            dataBucketName,
            [ "*" ],
            [ "GET", "PUT", "POST", "DELETE", "HEAD"],
            [.. origins],
            cancellationToken);
        if(!setDataBucketCorsConfig)
        {
            logger.LogError("Failed to set data bucket CORS configuration.");
            throw new Exception("Failed to set data bucket CORS configuration.");
        }

        logger.LogInformation("Creating initial user '{initialUserEmail}'.", options.InitialUserEmail);
        var createInitialUser = await cognitoService.CreateUserAsync(
            options.InitialUserEmail,
            userPoolId,
            tags,
            cancellationToken);
        if (!setDataBucketCorsConfig)
        {
            logger.LogError("Failed to create initial user.");
            throw new Exception("Failed to create initial user.");
        }

        return true;
    }

    private bool IsLocalstack()
    {
        return
            options.BaseUrl.Contains("localhost") == true ||
            options.BaseUrl.Contains("localstack") == true;
    }

    private string GetDistributionOrigin(string distributionDomain)
    {
        if (IsLocalstack())
        {
            return $"https://{distributionDomain}:8443";
        }
        else
        {
            return $"https://{distributionDomain}";
        }
    }

    private string GetBucketUrl(string bucketName)
    {
        var bucketNameWithPrefix = $"{options.ResourcePrefix}.{bucketName}";
        if (IsLocalstack())
        {
            return $"{options.BaseUrl}/{bucketNameWithPrefix}";
        }
        else
        {
            return $"https://{bucketNameWithPrefix}.s3.{options.Region}.amazonaws.com";
        }
    }

    private string GetPortalWebsiteConfigUrl(string bucketName)
    {
        var bucketNameWithPrefix = $"{options.ResourcePrefix}.{bucketName}";
        if (IsLocalstack())
        {
            return $"http://localhost:8080";
        }
        else
        {
            return $"http://{bucketNameWithPrefix}.s3-website-{options.Region}.amazonaws.com";
        }
    }
}
