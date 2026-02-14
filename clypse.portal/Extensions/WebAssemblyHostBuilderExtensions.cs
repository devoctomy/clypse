using clypse.portal.Models;
using clypse.portal.Models.Aws;
using clypse.portal.Models.Settings;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace clypse.portal.Extensions;

public static class WebAssemblyHostBuilderExtensions
{
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
