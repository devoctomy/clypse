using clypse.portal.setup.Enums;
using clypse.portal.setup.Extensions;
using clypse.portal.setup.Services.Build;
using clypse.portal.setup.Services.Cloudfront;
using clypse.portal.setup.Services.Cognito;
using clypse.portal.setup.Services.Iam;
using clypse.portal.setup.Services.Inventory;
using clypse.portal.setup.Services.IO;
using clypse.portal.setup.Services.S3;
using clypse.portal.setup.Services.Security;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace clypse.portal.setup.Services.Orchestration;

/// <inheritdoc cref="IClypseAwsSetupOrchestration" />
public class ClypseAwsSetupOrchestration(
        SetupOptions options,
        ISecurityTokenService securityTokenService,
        IIamService iamService,
        IS3Service s3Service,
        ICognitoService cognitoService,
        ICloudfrontService cloudfrontService,
        IPortalConfigService portalConfigService,
        IIoService ioService,
        IInventoryService inventoryService,
        ILogger<IamService> logger) : IClypseAwsSetupOrchestration
{
    /// <inheritdoc />
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

    /// <inheritdoc />
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
            true,
            cancellationToken);
        if (!createdPortalBucket)
        {
            logger.LogError("Failed to create S3 bucket for portal.");
            throw new Exception("Failed to create S3 bucket for portal.");
        }

        inventoryService.RecordResource(new()
        {
            Description = "S3 Bucket for portal website.",
            ResourceType = ResourceType.S3Bucket,
            ResourceId = $"{options.ResourcePrefix}.{portalBucketName}"
        });

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

        var publicReadPolicy = new
        {
            Version = "2012-10-17",
            Statement = new[]
            {
                new
                {
                    Sid = "PublicReadGetObject",
                    Effect = "Allow",
                    Principal = "*",
                    Action = "s3:GetObject",
                    Resource = $"arn:aws:s3:::{options.ResourcePrefix}.{portalBucketName}/*"
                }
            }
        };

        logger.LogInformation("Setting portal bucket policy.");
        var setPortalBucketPolicy = await s3Service.SetBucketPolicyAsync(
            portalBucketName,
            publicReadPolicy,
            cancellationToken);
        if (!setPortalBucketPolicy)
        {
            logger.LogError("Failed to set portal bucket policy.");
            throw new Exception("Failed to set portal bucket policy.");
        }

        logger.LogInformation("Creating S3 bucket for data.");
        var dataBucketName = "clypse.data";
        var createdDataBucket = await s3Service.CreateBucketAsync(
            dataBucketName,
            false,
            cancellationToken);
        if (!createdDataBucket)
        {
            logger.LogError("Failed to create S3 bucket for data.");
            throw new Exception("Failed to create S3 bucket for data.");
        }

        inventoryService.RecordResource(new()
        {
            Description = "S3 Bucket for user data.",
            ResourceType = ResourceType.S3Bucket,
            ResourceId = $"{options.ResourcePrefix}.{dataBucketName}"
        });

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

        logger.LogInformation("Creating data policy.");
        var dataPolicyArn = await iamService.CreatePolicyAsync(
            "clypse.data.policy",
            dataPolicyDocument,
            tags,
            cancellationToken);
        logger.LogInformation("Data policy '{dataPolicyArn}' created.", dataPolicyArn);

        inventoryService.RecordResource(new()
        {
            Description = "IAM Policy for user data.",
            ResourceType = ResourceType.IamPolicy,
            ResourceId = dataPolicyArn
        });

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
                    Resource = new[]
                    {
                        "*",
                    },
                }
            }
        };

        logger.LogInformation("Creating auth policy.");
        var authPolicyArn = await iamService.CreatePolicyAsync(
            "clypse.auth.policy",
            authPolicyDocument,
            tags,
            cancellationToken);
        logger.LogInformation("Auth policy '{authPolicyArn}' created.", authPolicyArn);

        inventoryService.RecordResource(new()
        {
            Description = "IAM Policy for cognito auth.",
            ResourceType = ResourceType.IamPolicy,
            ResourceId = authPolicyArn
        });

        // Cognito
        logger.LogInformation("Setting up Cognito resources.");

        logger.LogInformation("Creating user pool.");
        var userPoolId = await cognitoService.CreateUserPoolAsync(
            "clypse.user.pool",
            tags,
            cancellationToken);
        logger.LogInformation("User pool '{userPoolId}' created.", userPoolId);

        inventoryService.RecordResource(new()
        {
            Description = "Cognito user pool.",
            ResourceType = ResourceType.CognitoUserPool,
            ResourceId = userPoolId
        });

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

        inventoryService.RecordResource(new()
        {
            Description = "Cognito user pool client.",
            ResourceType = ResourceType.CognitoUserPoolClient,
            ResourceId = userPoolClientId
        });

        logger.LogInformation("Creating identity pool.");
        var identityPoolId = await cognitoService.CreateIdentityPoolAsync(
            "clypse.identity.pool",
            userPoolId,
            userPoolClientId,
            tags,
            cancellationToken);
        logger.LogInformation("Identity pool '{userPoolId}' created.", userPoolId);

        inventoryService.RecordResource(new()
        {
            Description = "Cognito identity pool.",
            ResourceType = ResourceType.CognitoIdentityPool,
            ResourceId = identityPoolId
        });

        // IAM Role for Authenticated Users, needs to be created after identity pool
        logger.LogInformation("Creating auth role.");
        var authRoleArn = await iamService.CreateRoleAsync(
           "clypse.auth.role",
           GetTrustPolicyJson(identityPoolId),
           tags,
           cancellationToken);
        logger.LogInformation("Auth role '{authRoleArn}' created.", authRoleArn);

        inventoryService.RecordResource(new()
        {
            Description = "IAM role for authenticated cognito users.",
            ResourceType = ResourceType.IamPolicy,
            ResourceId = identityPoolId
        });

        logger.LogInformation("Attaching data policy to auth role.");
        var attachDataPolicyToAuthRole = await iamService.AttachPolicyToRoleAsync(
            "clypse.auth.role",
            dataPolicyArn,
            cancellationToken);
        if (!attachDataPolicyToAuthRole)
        {
            logger.LogError("Failed to attach data policy to auth role.");
            throw new Exception("Failed to attach data policy to auth role.");
        }

        logger.LogInformation("Attaching auth policy to auth role.");
        var attachAuthPolicyToAuthRole = await iamService.AttachPolicyToRoleAsync(
            "clypse.auth.role",
            authPolicyArn,
            cancellationToken);
        if (!attachAuthPolicyToAuthRole)
        {
            logger.LogError("Failed to attach auth policy to auth role.");
            throw new Exception("Failed to attach auth policy to auth role.");
        }

        logger.LogInformation("Setting identity pool authenticated role as '{authRoleArn}'.", authRoleArn);
        var setAuthenticatedRole = await cognitoService.SetIdentityPoolAuthenticatedRoleAsync(
            identityPoolId,
            authRoleArn,
            cancellationToken);
        if (!setAuthenticatedRole)
        {
            logger.LogError("Failed to set authenticated role for identity pool.");
            throw new Exception("Failed to set authenticated role for identity pool.");
        }

        await DeployPortal(
            portalBucketName,
            dataBucketName,
            userPoolId,
            userPoolClientId,
            identityPoolId,
            cancellationToken);

        logger.LogInformation("Setting portal bucket website configuration.");
        var setBucketWebsiteConfig = await s3Service.SetBucketWebsiteConfigurationAsync(
            portalBucketName,
            cancellationToken: cancellationToken);
        if (!setBucketWebsiteConfig)
        {
            logger.LogError("Failed to set portal bucket website configuration.");
            throw new Exception("Failed to set portal bucket website configuration.");
        }

        var portalBucketUrl = $"https://{options.ResourcePrefix}.{portalBucketName}.s3.{options.Region}.amazonaws.com";
        logger.LogInformation("Portal Bucket Url : {bucketUrl}", portalBucketUrl);

        var portalWebsiteUrl = $"http://{options.ResourcePrefix}.{portalBucketName}.s3-website-{options.Region}.amazonaws.com";
        logger.LogInformation("Portal Website Url : {portalWebsiteUrl}", portalWebsiteUrl);

        logger.LogInformation("Creating CloudFront distribution.");
        var aliasProvided = !string.IsNullOrWhiteSpace(options.Alias);
        var certificateProvided = !string.IsNullOrWhiteSpace(options.CertificateArn);
        var useCustomDomain = aliasProvided && certificateProvided;

        if (useCustomDomain)
        {
            logger.LogInformation("CloudFront alias '{alias}' and certificate ARN '{certificateArn}' provided; these will be used. HTTP endpoints are strongly discouraged—use the CloudFront address over HTTPS.", options.Alias, options.CertificateArn);
        }
        else if (aliasProvided || certificateProvided)
        {
            logger.LogWarning("Only one of CloudFront alias ('{alias}') or certificate ARN ('{certificateArn}') was provided; neither will be used. HTTP endpoints are strongly discouraged—use the CloudFront distribution address over HTTPS if you don't have a certificate.", options.Alias, options.CertificateArn);
        }
        else
        {
            logger.LogInformation("No CloudFront alias or certificate provided; using the default CloudFront domain. HTTP endpoints are strongly discouraged—use the CloudFront distribution address over HTTPS if you don't have a certificate.");
        }

        var distributionDomain = await cloudfrontService.CreateDistributionAsync(
            portalWebsiteUrl.Replace("http://", string.Empty),
            useCustomDomain ? options.Alias : null,
            useCustomDomain ? options.CertificateArn : null,
            cancellationToken: cancellationToken);
        if (string.IsNullOrEmpty(distributionDomain))
        {
            logger.LogError("Failed to create CloudFront distribution.");
            throw new Exception("Failed to create CloudFront distribution.");
        }

        inventoryService.RecordResource(new()
        {
            Description = "Cloudfront distribution for HTTPS support.",
            ResourceType = ResourceType.CloudFrontDistribution,
            ResourceId = distributionDomain
        });

        var origins = (string[])["http://localhost:8080", $"https://{distributionDomain}"];

        logger.LogInformation("Setting data bucket CORS configuration with origins ({origins}).", string.Join(',', origins));
        var setDataBucketCorsConfig = await s3Service.SetBucketCorsConfigurationAsync(
            dataBucketName,
            ["*"],
            ["GET", "PUT", "POST", "DELETE", "HEAD"],
            [.. origins],
            cancellationToken);
        if (!setDataBucketCorsConfig)
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
        if (!createInitialUser)
        {
            logger.LogError("Failed to create initial user.");
            throw new Exception("Failed to create initial user.");
        }

        var inventoryFilePath = $"{setupId}-inventory.json";
        inventoryService.Save(inventoryFilePath);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> UpgradePortalAsync(CancellationToken cancellationToken)
    {
        // download current app settings from bucket and extract values
        // deploy portal with new settings
        throw new NotImplementedException();
    }

    private async Task DeployPortal(
        string portalBucketName,
        string dataBucketName,
        string userPoolId,
        string userPoolClientId,
        string identityPoolId,
        CancellationToken cancellationToken)
    {
        if (Directory.Exists(options.PortalBuildOutputPath))
        {
            logger.LogInformation("Removing unwanted settings from build output.");
            var oldSettings = ioService.GetFiles(options.PortalBuildOutputPath, "appsettings*");
            foreach (var oldSetting in oldSettings)
            {
                logger.LogInformation("Removing portal setting file '{oldSetting}'.", oldSetting);
                ioService.Delete(oldSetting);
            }

            logger.LogInformation("Reconfiguring portal.");
            using var configStream = await portalConfigService.ConfigureAsync(
                "Data/appsettings.json",
                $"{options.ResourcePrefix}.{dataBucketName}",
                options.Region,
                userPoolId,
                userPoolClientId,
                options.Region,
                identityPoolId,
                cancellationToken);

            var configFilePath = ioService.CombinePath(options.PortalBuildOutputPath, "appsettings.json");
            using var outputStream = ioService.OpenWrite(configFilePath);
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
    }

    private static string GetTrustPolicyJson(string identityPoolId)
    {
        var trustPolicy = new
        {
            Version = "2012-10-17",
            Statement = new[]
            {
            new
            {
                Effect = "Allow",
                Principal = new
                {
                    Federated = "cognito-identity.amazonaws.com"
                },
                Action = "sts:AssumeRoleWithWebIdentity",
                Condition = new Dictionary<string, object>
                {
                    ["StringEquals"] = new Dictionary<string, string>
                    {
                        ["cognito-identity.amazonaws.com:aud"] = identityPoolId
                    },
                    ["ForAnyValue:StringLike"] = new Dictionary<string, string>
                    {
                        ["cognito-identity.amazonaws.com:amr"] = "authenticated"
                    }
                }
            }
        }
        };

        return JsonSerializer.Serialize(trustPolicy);
    }
}
