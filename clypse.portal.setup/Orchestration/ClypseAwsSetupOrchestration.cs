using Amazon.Runtime.Internal.Util;
using clypse.portal.setup.Cognito;
using clypse.portal.setup.Extensions;
using clypse.portal.setup.Iam;
using clypse.portal.setup.S3;
using Microsoft.Extensions.Logging;

namespace clypse.portal.setup.Orchestration;

internal class ClypseAwsSetupOrchestration(
        AwsServiceOptions options,
        IIamService iamService,
        IS3Service s3Service,
        ICognitoService cognitoService,
        ILogger<IamService> logger) : IClypseAwsSetupOrchestration
{
    public async Task SetupClypseOnAwsAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting Clypse AWS setup orchestration.");

        logger.LogDebug("Using AWS Base Url: {baseUrl}", options.BaseUrl);
        logger.LogDebug("Using AWS Region: {region}", options.Region);
        logger.LogDebug("Using Resource Prefix: {resourcePrefix}", options.ResourcePrefix);
        logger.LogDebug("Using IAM Access Id: {accessId}", options.AccessId);
        logger.LogDebug("Using IAM Secret Access Key: {secretAccessKey}", options.SecretAccessKey.Redact(3));

        if(!options.IsValid())
        {
            throw new Exception("Options are not valid.");
        }

        if (options.InteractiveMode)
        {
            logger.LogInformation("Press any key to begin.");
            Console.ReadKey();
        }

        // S3
        logger.LogInformation("Setting up S3 resources.");
        logger.LogInformation("Creating S3 bucket for portal.");
        var createdPortalBucket = await s3Service.CreateBucketAsync("clypse.portal", cancellationToken);
        if(!createdPortalBucket)
        {
            logger.LogError("Failed to create S3 bucket for portal.");
            throw new Exception("Failed to create S3 bucket for portal.");
        }

        logger.LogInformation("Creating S3 bucket for data.");
        var dataBucketName = "clypse.data";
        var createdDataBucket = await s3Service.CreateBucketAsync(dataBucketName, cancellationToken);
        if (!createdDataBucket)
        {
            logger.LogError("Failed to create S3 bucket for data.");
            throw new Exception("Failed to create S3 bucket for data.");
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
        var dataPolicyArn = await iamService.CreatePolicyAsync("clypse.data.policy", dataPolicyDocument, cancellationToken);
        logger.LogInformation("Data policy '{dataPolicyArn}' created.", dataPolicyArn);

        logger.LogInformation("Creating auth policy.");
        var authPolicyArn = await iamService.CreatePolicyAsync("clypse.auth.policy", authPolicyDocument, cancellationToken);
        logger.LogInformation("Auth policy '{authPolicyArn}' created.", authPolicyArn);

        // Cognito
        logger.LogInformation("Setting up Cognito resources.");

        logger.LogInformation("Creating user pool.");
        var userPoolId = await cognitoService.CreateUserPoolAsync("clypse.user.pool", cancellationToken);
        logger.LogInformation("User pool '{userPoolId}' created.", userPoolId);

        logger.LogInformation("Creating identity pool.");
        var identityPoolId = await cognitoService.CreateIdentityPoolAsync("clypse.identity.pool", cancellationToken);
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

        // setup cloudfront resources (required for https delivery of portal)

        // deploy portal
    }
}
