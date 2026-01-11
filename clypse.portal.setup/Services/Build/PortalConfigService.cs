using System.Text.Json;
using System.Text.Json.Nodes;

namespace clypse.portal.setup.Services.Build;

public class PortalConfigService : IPortalConfigService
{
    public async Task<MemoryStream> ConfigureAsync(
        string templatePath,
        string s3DataBucketName,
        string s3Region,
        string cognitoUserPoolId,
        string cognitoUserPoolClientId,
        string cognitoRegion,
        string cognitoIdentityPoolId,
        CancellationToken cancellationToken = default)
    {
        var templateRaw = await File.ReadAllTextAsync(templatePath, cancellationToken);
        var templateJson =
            JsonNode.Parse(templateRaw) ??
            throw new Exception("Failed to parse template JSON.");
        
        templateJson["AwsS3"]!["BucketName"] = s3DataBucketName;
        templateJson["AwsS3"]!["Region"] = s3Region;
        templateJson["AwsCognito"]!["UserPoolId"] = cognitoUserPoolId;
        templateJson["AwsCognito"]!["UserPoolClientId"] = cognitoUserPoolClientId;
        templateJson["AwsCognito"]!["Region"] = cognitoRegion;
        templateJson["AwsCognito"]!["IdentityPoolId"] = cognitoIdentityPoolId;

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
