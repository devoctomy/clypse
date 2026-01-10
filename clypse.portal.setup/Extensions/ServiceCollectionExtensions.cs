using Amazon.CloudFront;
using Amazon.CognitoIdentity;
using Amazon.CognitoIdentityProvider;
using Amazon.IdentityManagement;
using Amazon.S3;
using Amazon.SecurityToken;
using clypse.portal.setup;
using clypse.portal.setup.Services;
using clypse.portal.setup.Services.Build;
using clypse.portal.setup.Services.Cloudfront;
using clypse.portal.setup.Services.Cognito;
using clypse.portal.setup.Services.Iam;
using clypse.portal.setup.Services.Orchestration;
using clypse.portal.setup.Services.S3;
using clypse.portal.setup.Services.Security;
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
    /// <param name="logLevel">The log level to configure for setup orchestration.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddClypseSetupServices(
        this IServiceCollection services,
        Microsoft.Extensions.Logging.LogLevel logLevel = Microsoft.Extensions.Logging.LogLevel.Debug)
    {
        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        var options = new AwsServiceOptions();
        configuration
            .GetSection("CLYPSE_SETUP")
            .Bind(options);

        ApplyWindowsUserEnvironmentFallback(options);
        services.AddSingleton(options);

        services.AddLogging(logging =>
        {
            logging.AddConsole();
            logging.SetMinimumLevel(logLevel);
        });

        services.AddScoped<IAmazonSecurityTokenService>((sp) =>
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

        services.AddScoped<IAmazonS3>((sp) =>
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

        services.AddScoped<IAmazonCognitoIdentity>((sp) =>
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

        services.AddScoped<IAmazonCognitoIdentityProvider>((sp) =>
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

        services.AddScoped<IAmazonIdentityManagementService>((sp) =>
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

        services.AddScoped<IAmazonCloudFront>((sp) =>
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

        services.AddScoped<ISecurityTokenService, SecurityTokenService>();
        services.AddScoped<IS3Service, S3Service>();
        services.AddScoped<ICognitoService, CognitoService>();
        services.AddScoped<IIamService, IamService>();
        services.AddScoped<ICloudfrontService, CloudfrontService>();
        services.AddScoped<IPortalBuildService, PortalBuildService>();
        services.AddScoped<ISetupInteractiveMenuService, SetupInteractiveMenuService>();
        services.AddScoped<IClypseAwsSetupOrchestration, ClypseAwsSetupOrchestration>();
        services.AddSingleton<IProgram, SetupProgram>();

        return services;
    }

    private static void ApplyWindowsUserEnvironmentFallback(AwsServiceOptions options)
    {
        if (!OperatingSystem.IsWindows())   // !!! TODO: This should be abstracted into a service to allow for proper unit testing
        {
            return;
        }

        options.BaseUrl = PreferExisting(
            options.BaseUrl,
            Environment.GetEnvironmentVariable("CLYPSE_SETUP__BaseUrl", EnvironmentVariableTarget.User));
        options.AccessId = PreferExisting(
            options.AccessId,
            Environment.GetEnvironmentVariable("CLYPSE_SETUP__AccessId", EnvironmentVariableTarget.User));
        options.SecretAccessKey = PreferExisting(
            options.SecretAccessKey,
            Environment.GetEnvironmentVariable("CLYPSE_SETUP__SecretAccessKey", EnvironmentVariableTarget.User));
        options.Region = PreferExisting(
            options.Region,
            Environment.GetEnvironmentVariable("CLYPSE_SETUP__Region", EnvironmentVariableTarget.User));
        options.ResourcePrefix = PreferExisting(
            options.ResourcePrefix,
            Environment.GetEnvironmentVariable("CLYPSE_SETUP__ResourcePrefix", EnvironmentVariableTarget.User));
        options.InitialUserEmail = PreferExisting(
            options.InitialUserEmail,
            Environment.GetEnvironmentVariable("CLYPSE_SETUP__InitialUserEmail", EnvironmentVariableTarget.User));
        options.PortalBuildOutputPath = PreferExisting(
            options.PortalBuildOutputPath,
            Environment.GetEnvironmentVariable("CLYPSE_SETUP__PortalBuildOutputPath", EnvironmentVariableTarget.User));
    }

    private static string PreferExisting(string currentValue, string? fallbackValue)
    {
        return string.IsNullOrWhiteSpace(currentValue)
            ? (fallbackValue ?? string.Empty)
            : currentValue;
    }
}
