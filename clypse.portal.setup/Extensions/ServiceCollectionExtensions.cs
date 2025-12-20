using Amazon.CognitoIdentity;
using Amazon.CognitoIdentityProvider;
using Amazon.S3;
using clypse.portal.setup;
using clypse.portal.setup.Cognito;
using clypse.portal.setup.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace clypse.core.Extensions;

/// <summary>
/// Extension methods for IServiceCollection to add Clypse core services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Clypse setup services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddClypseSetupServices(this IServiceCollection services)
    {
        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        var options = new AwsServiceOptions();
        configuration
            .GetSection("CLYPSE_SETUP")
            .Bind(options);
        services.AddSingleton(options);

        services.AddLogging(logging =>
        {
            logging.AddConsole();
            logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);
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
                options.AccessKey,
                options.SecretKey,
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
                options.AccessKey,
                options.SecretKey,
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
                options.AccessKey,
                options.SecretKey,
                config);
        });

        services.AddScoped<IS3Service, S3Service>();
        services.AddScoped<ICognitoService, CognitoService>();

        return services;
    }
}
