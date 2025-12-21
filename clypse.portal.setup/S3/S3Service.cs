using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace clypse.portal.setup.S3;

public class S3Service(
    IAmazonS3 amazonS3,
    AwsServiceOptions options,
    ILogger<S3Service> logger) : IS3Service
{
    private static JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
    {
        WriteIndented = false,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };

    public async Task<bool> CreateBucketAsync(
        string bucketName,
        CancellationToken cancellationToken = default)
    {
        var bucketNameWithPrefix = $"{options.ResourcePrefix}.{bucketName}";
        logger.LogInformation("Creating S3 bucket: {BucketName}", bucketNameWithPrefix);

        var putBucketRequest = new PutBucketRequest
        {
            BucketName = bucketNameWithPrefix
        };
        var response = await amazonS3.PutBucketAsync(putBucketRequest, cancellationToken);
        return
            response.HttpStatusCode == System.Net.HttpStatusCode.OK ||
            response.HttpStatusCode == System.Net.HttpStatusCode.Created;
    }

    /// <summary>
    /// Sets the CORS configuration for the specified S3 bucket.
    /// </summary>
    /// <param name="bucketName">Name of the bucket to configure.</param>
    /// <param name="allowedHeaders"></param>
    /// <param name="allowedMethods"></param>
    /// <param name="allowedOrigins"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<bool> SetBucketCorsConfigurationAsync(
        string bucketName,
        List<string> allowedHeaders,
        List<string> allowedMethods,
        List<string> allowedOrigins,
        CancellationToken cancellationToken = default)
    {
        var bucketNameWithPrefix = $"{options.ResourcePrefix}.{bucketName}";
        logger.LogInformation("Setting CORS configuration for bucket: {BucketName}", bucketNameWithPrefix);

        var corsConfiguration = new CORSConfiguration
        {
            Rules =
            [
                new CORSRule
                {
                    AllowedHeaders = allowedHeaders, // e.g. ["*"],
                    AllowedMethods = allowedMethods, // e.g. ["GET", "POST", "PUT", "DELETE", "HEAD"],
                    AllowedOrigins = allowedOrigins, // e.g. ["*"], 
                    ExposeHeaders = ["ETag"],
                    MaxAgeSeconds = 3000
                }
            ]
        };

        var response = await amazonS3.PutCORSConfigurationAsync(
            bucketNameWithPrefix,
            corsConfiguration,
            cancellationToken);
        return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
    }

    public async Task<bool> SetBucketPolicyAsync(
        string bucketName,
        object policyDocument,
        CancellationToken cancellationToken = default)
    {
        var bucketNameWithPrefix = $"{options.ResourcePrefix}.{bucketName}";
        logger.LogInformation("Setting policy for bucket: {BucketName}", bucketNameWithPrefix);

        var putBucketPolicyRequest = new PutBucketPolicyRequest
        {
            BucketName = bucketNameWithPrefix,
            Policy = JsonSerializer.Serialize(policyDocument, _jsonSerializerOptions)
        };

        var response = await amazonS3.PutBucketPolicyAsync(
            putBucketPolicyRequest,
            cancellationToken);
        return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
    }

}
