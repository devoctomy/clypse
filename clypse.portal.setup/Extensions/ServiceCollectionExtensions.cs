using Amazon.CloudFront;
using Amazon.CognitoIdentity;
using Amazon.CognitoIdentityProvider;
using Amazon.IdentityManagement;
using Amazon.S3;
using Amazon.SecurityToken;
using clypse.portal.setup.Services;
using clypse.portal.setup.Services.Build;
using clypse.portal.setup.Services.Cloudfront;
using clypse.portal.setup.Services.Cognito;
using clypse.portal.setup.Services.Environment;
using clypse.portal.setup.Services.Iam;
using clypse.portal.setup.Services.Inventory;
using clypse.portal.setup.Services.IO;
using clypse.portal.setup.Services.Json;
using clypse.portal.setup.Services.Orchestration;
using clypse.portal.setup.Services.Process;
using clypse.portal.setup.Services.S3;
using clypse.portal.setup.Services.Security;
using clypse.portal.setup.Services.Upload;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace clypse.portal.setup.Extensions;

/// <summary>
/// Extension methods for IServiceCollection to add Clypse portal setup services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Clypse setup services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="environmentVariablePrefix">Environment variable prefix used to bind <see cref="SetupOptions"/>.</param>
    /// <param name="environmentService">Optional environment service for testability. If null, uses default implementation.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddClypseSetupServices(
        this IServiceCollection services,
        string environmentVariablePrefix = "CLYPSE_SETUP",
        Microsoft.Extensions.Logging.LogLevel logLevel = Microsoft.Extensions.Logging.LogLevel.Debug,
        IEnvironmentService? environmentService = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        var options = new SetupOptions();
        configuration
            .GetSection(environmentVariablePrefix)
            .Bind(options);

        environmentService ??= new EnvironmentService();
        ApplyWindowsUserEnvironmentFallback(options, environmentVariablePrefix, environmentService);
        services.AddSingleton(options);
        services.AddSingleton<IEnvironmentService>(environmentService);

        services.AddLogging(logging =>
        {
            logging.AddConsole();
            logging.SetMinimumLevel(logLevel);
        });

        services.AddScoped<IAmazonSecurityTokenService>((_) =>
        {
            var config = new AmazonSecurityTokenServiceConfig
            {
                RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(options.Region)
            };

            if (!string.IsNullOrWhiteSpace(options.BaseUrl))
            {
                config.ServiceURL = options.BaseUrl;
            }

            return new AmazonSecurityTokenServiceClient(
                options.AccessId,
                options.SecretAccessKey,
                config);
        });

        services.AddScoped<IAmazonS3>((_) =>
        {
            var config = new AmazonS3Config
            {
                RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(options.Region)
            };

            if (!string.IsNullOrWhiteSpace(options.BaseUrl))
            {
                config.ServiceURL = options.BaseUrl;
                config.ForcePathStyle = true;
            }

            return new AmazonS3Client(
                options.AccessId,
                options.SecretAccessKey,
                config);
        });

        services.AddScoped<IAmazonCognitoIdentity>((_) =>
        {
            var config = new AmazonCognitoIdentityConfig
            {
                RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(options.Region)
            };

            if (!string.IsNullOrWhiteSpace(options.BaseUrl))
            {
                config.ServiceURL = options.BaseUrl;
            }

            return new AmazonCognitoIdentityClient(
                options.AccessId,
                options.SecretAccessKey,
                config);
        });

        services.AddScoped<IAmazonCognitoIdentityProvider>((_) =>
        {
            var config = new AmazonCognitoIdentityProviderConfig
            {
                RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(options.Region)
            };

            if (!string.IsNullOrWhiteSpace(options.BaseUrl))
            {
                config.ServiceURL = options.BaseUrl;
            }

            return new AmazonCognitoIdentityProviderClient(
                options.AccessId,
                options.SecretAccessKey,
                config);
        });

        services.AddScoped<IAmazonIdentityManagementService>((_) =>
        {
            var config = new AmazonIdentityManagementServiceConfig
            {
                RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(options.Region)
            };

            if (!string.IsNullOrWhiteSpace(options.BaseUrl))
            {
                config.ServiceURL = options.BaseUrl;
            }
            
            return new AmazonIdentityManagementServiceClient(
                options.AccessId,
                options.SecretAccessKey,
                config);
        });

        services.AddScoped<IAmazonCloudFront>((_) =>
        {
            var config = new AmazonCloudFrontConfig
            {
                RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(options.Region)
            };

            if (!string.IsNullOrWhiteSpace(options.BaseUrl))
            {
                config.ServiceURL = options.BaseUrl;
            }

            return new AmazonCloudFrontClient(
                options.AccessId,
                options.SecretAccessKey,
                config);
        });

        services.AddScoped<IIoService, IoService>();
        services.AddScoped<IProcessRunnerService, ProcessRunnerService>();
        services.AddScoped<ISecurityTokenService, SecurityTokenService>();
        services.AddScoped<IS3Service, S3Service>();
        services.AddScoped<IDirectoryUploadService, AmazonS3TransferUtilityDirectoryUploadService>();
        services.AddScoped<ICognitoService, CognitoService>();
        services.AddScoped<IIamService, IamService>();
        services.AddScoped<ICloudfrontService, CloudfrontService>();
        services.AddScoped<IPortalBuildService, PortalBuildService>();
        services.AddScoped<ISetupInteractiveMenuService, SetupInteractiveMenuService>();
        services.AddScoped<IClypseAwsSetupOrchestration, ClypseAwsSetupOrchestration>();
        services.AddScoped<IPortalConfigService, PortalConfigService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IJsonMergerService, NewtonsoftJsonMergerService>();
        services.AddScoped<IServiceWorkerAssetHashUpdaterService, ServiceWorkerAssetHashUpdaterService>();
        services.AddSingleton<IProgram, SetupProgram>();

        return services;
    }

    private static void ApplyWindowsUserEnvironmentFallback(SetupOptions options, string environmentVariablePrefix, IEnvironmentService environmentService)
    {
        if (!environmentService.IsWindows)
        {
            return;
        }

        options.BaseUrl = PreferExisting(
            options.BaseUrl,
            environmentService.GetEnvironmentVariable("CLYPSE_SETUP__BaseUrl", EnvironmentVariableTarget.User));
        options.AccessId = PreferExisting(
            options.AccessId,
            environmentService.GetEnvironmentVariable("CLYPSE_SETUP__AccessId", EnvironmentVariableTarget.User));
        options.SecretAccessKey = PreferExisting(
            options.SecretAccessKey,
            environmentService.GetEnvironmentVariable("CLYPSE_SETUP__SecretAccessKey", EnvironmentVariableTarget.User));
        options.Region = PreferExisting(
            options.Region,
            environmentService.GetEnvironmentVariable("CLYPSE_SETUP__Region", EnvironmentVariableTarget.User));
        options.ResourcePrefix = PreferExisting(
            options.ResourcePrefix,
            environmentService.GetEnvironmentVariable("CLYPSE_SETUP__ResourcePrefix", EnvironmentVariableTarget.User));
        options.Alias = PreferExisting(
            options.Alias,
            environmentService.GetEnvironmentVariable("CLYPSE_SETUP__Alias", EnvironmentVariableTarget.User));
        options.CertificateArn = PreferExisting(
            options.CertificateArn,
            environmentService.GetEnvironmentVariable("CLYPSE_SETUP__CertificateArn", EnvironmentVariableTarget.User));
        options.InitialUserEmail = PreferExisting(
            options.InitialUserEmail,
            environmentService.GetEnvironmentVariable("CLYPSE_SETUP__InitialUserEmail", EnvironmentVariableTarget.User));
        options.PortalBuildOutputPath = PreferExisting(
            options.PortalBuildOutputPath,
            environmentService.GetEnvironmentVariable("CLYPSE_SETUP__PortalBuildOutputPath", EnvironmentVariableTarget.User));
        options.CloudFrontDistributionId = PreferExisting(
            options.CloudFrontDistributionId,
            environmentService.GetEnvironmentVariable("CLYPSE_SETUP__CloudFrontDistributionId", EnvironmentVariableTarget.User));
        
        // Boolean properties - only override if explicitly set in user environment and not already set via configuration
        // Check if process-level env var is set first to avoid overriding configuration binding
        var processInteractiveMode = environmentService.GetEnvironmentVariable($"{environmentVariablePrefix}__InteractiveMode");
        if (string.IsNullOrWhiteSpace(processInteractiveMode))
        {
            var interactiveModeEnv = environmentService.GetEnvironmentVariable("CLYPSE_SETUP__InteractiveMode", EnvironmentVariableTarget.User);
            if (!string.IsNullOrWhiteSpace(interactiveModeEnv) && bool.TryParse(interactiveModeEnv, out var interactiveMode))
            {
                options.InteractiveMode = interactiveMode;
            }
        }

        var processEnableUpgradeMode = environmentService.GetEnvironmentVariable($"{environmentVariablePrefix}__EnableUpgradeMode");
        if (string.IsNullOrWhiteSpace(processEnableUpgradeMode))
        {
            var enableUpgradeModeEnv = environmentService.GetEnvironmentVariable("CLYPSE_SETUP__EnableUpgradeMode", EnvironmentVariableTarget.User);
            if (!string.IsNullOrWhiteSpace(enableUpgradeModeEnv) && bool.TryParse(enableUpgradeModeEnv, out var enableUpgradeMode))
            {
                options.EnableUpgradeMode = enableUpgradeMode;
            }
        }

        var processBuildPortal = environmentService.GetEnvironmentVariable($"{environmentVariablePrefix}__BuildPortal");
        if (string.IsNullOrWhiteSpace(processBuildPortal))
        {
            var buildPortalEnv = environmentService.GetEnvironmentVariable("CLYPSE_SETUP__BuildPortal", EnvironmentVariableTarget.User);
            if (!string.IsNullOrWhiteSpace(buildPortalEnv) && bool.TryParse(buildPortalEnv, out var buildPortal))
            {
                options.BuildPortal = buildPortal;
            }
        }

        var processForceUpgrade = environmentService.GetEnvironmentVariable($"{environmentVariablePrefix}__ForceUpgrade");
        if (string.IsNullOrWhiteSpace(processForceUpgrade))
        {
            var forceUpgradeEnv = environmentService.GetEnvironmentVariable("CLYPSE_SETUP__ForceUpgrade", EnvironmentVariableTarget.User);
            if (!string.IsNullOrWhiteSpace(forceUpgradeEnv) && bool.TryParse(forceUpgradeEnv, out var forceUpgrade))
            {
                options.ForceUpgrade = forceUpgrade;
            }
        }
    }

    private static string PreferExisting(string currentValue, string? fallbackValue)
    {
        return string.IsNullOrWhiteSpace(currentValue)
            ? (fallbackValue ?? string.Empty)
            : currentValue;
    }
}
