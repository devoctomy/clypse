using clypse.portal.Models.Aws;
using clypse.portal.Models.Settings;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace clypse.portal.Application.Extensions;

/// <summary>
/// Extension methods for configuring settings in a <see cref="WebAssemblyHostBuilder"/>.
/// </summary>
public static class WebAssemblyHostBuilderExtensions
{
    /// <summary>
    /// Retrieves and configures application settings from configuration and registers them as singletons.
    /// </summary>
    /// <param name="builder">The WebAssembly host builder.</param>
    /// <returns>A tuple containing the configured AWS Cognito, AWS S3, and application settings.</returns>
    public static (AwsCognitoConfig CognitoConfig, AwsS3Config S3Config, AppSettings AppSettings) GetSettings(this WebAssemblyHostBuilder builder)
    {
        // Configure AWS Cognito settings from appsettings.json
        var cognitoConfig = new AwsCognitoConfig();
        builder.Configuration.GetSection("AwsCognito").Bind(cognitoConfig);
        builder.Services.AddSingleton(cognitoConfig);

        // Configure AWS S3 settings from appsettings.json
        var awsS3Config = new AwsS3Config();
        builder.Configuration.GetSection("AwsS3").Bind(awsS3Config);
        builder.Services.AddSingleton(awsS3Config);

        // Configure App settings from appsettings.json
        var appSettings = new AppSettings();
        builder.Configuration.GetSection("AppSettings").Bind(appSettings);
        builder.Services.AddSingleton(appSettings);

        return (cognitoConfig, awsS3Config, appSettings);
    }
}
