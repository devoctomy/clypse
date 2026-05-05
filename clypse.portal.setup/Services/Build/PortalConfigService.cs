using clypse.portal.setup.Services.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace clypse.portal.setup.Services.Build;

/// <inheritdoc cref="IPortalConfigService" />
public class PortalConfigService(
    IIoService ioService) : IPortalConfigService
{
    /// <inheritdoc />
    public Task<MemoryStream> ConfigureAsync(
        string templatePath,
        string s3DataBucketName,
        string s3Region,
        string cognitoUserPoolId,
        string cognitoUserPoolClientId,
        string cognitoRegion,
        string cognitoIdentityPoolId,
        CancellationToken cancellationToken = default)
    {
        return ConfigureAsync(
            templatePath,
            s3DataBucketName,
            s3Region,
            cognitoUserPoolId,
            cognitoUserPoolClientId,
            cognitoRegion,
            cognitoIdentityPoolId,
            null,
            null,
            null,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<MemoryStream> ConfigureAsync(
        string templatePath,
        string s3DataBucketName,
        string s3Region,
        string cognitoUserPoolId,
        string cognitoUserPoolClientId,
        string cognitoRegion,
        string cognitoIdentityPoolId,
        string? deployedBy,
        string? deployedAt,
        string? deploymentActionUrl,
        CancellationToken cancellationToken = default)
    {
        var templateRaw = await ioService.ReadAllTextAsync(templatePath, cancellationToken);
        var templateJson =
            JsonNode.Parse(templateRaw) ??
            throw new Exception("Failed to parse template JSON.");
        
        templateJson["AwsS3"]!["BucketName"] = s3DataBucketName;
        templateJson["AwsS3"]!["Region"] = s3Region;
        templateJson["AwsCognito"]!["UserPoolId"] = cognitoUserPoolId;
        templateJson["AwsCognito"]!["UserPoolClientId"] = cognitoUserPoolClientId;
        templateJson["AwsCognito"]!["Region"] = cognitoRegion;
        templateJson["AwsCognito"]!["IdentityPoolId"] = cognitoIdentityPoolId;

        // Set deployment metadata if provided, nested under AppSettings.Deployment
        if (!string.IsNullOrWhiteSpace(deployedBy))
        {
            templateJson["AppSettings"]!["Deployment"]!["DeployedBy"] = deployedBy;
        }
        if (!string.IsNullOrWhiteSpace(deployedAt))
        {
            templateJson["AppSettings"]!["Deployment"]!["DeployedAt"] = deployedAt;
        }
        if (!string.IsNullOrWhiteSpace(deploymentActionUrl))
        {
            templateJson["AppSettings"]!["Deployment"]!["DeploymentActionUrl"] = deploymentActionUrl;
        }

        var outputStream = new MemoryStream();
        await using var outputJsonWriter = new Utf8JsonWriter(outputStream, new JsonWriterOptions
        {
            Indented = true,
        });

        templateJson.WriteTo(outputJsonWriter);
        await outputJsonWriter.FlushAsync(cancellationToken);
        outputStream.Seek(0, SeekOrigin.Begin);

        return outputStream;
    }
}
