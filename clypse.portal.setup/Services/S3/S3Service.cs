using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace clypse.portal.setup.Services.S3;

/// <summary>
/// Provides functionality for managing Amazon S3 buckets and their configurations.
/// </summary>
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

    /// <summary>
    /// Creates a new S3 bucket with the specified name.
    /// </summary>
    /// <param name="bucketName">The name of the bucket to create (without the resource prefix).</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>True if the bucket was created successfully; otherwise, false.</returns>
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
    /// <param name="bucketName">Name of the bucket to configure (without the resource prefix).</param>
    /// <param name="allowedHeaders">List of allowed headers in CORS requests (e.g., ["*"]).</param>
    /// <param name="allowedMethods">List of allowed HTTP methods (e.g., ["GET", "POST", "PUT", "DELETE", "HEAD"]).</param>
    /// <param name="allowedOrigins">List of allowed origins for CORS requests (e.g., ["https://example.com"]).</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>True if the CORS configuration was set successfully; otherwise, false.</returns>
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

    /// <summary>
    /// Sets the bucket policy for the specified S3 bucket.
    /// </summary>
    /// <param name="bucketName">Name of the bucket to configure (without the resource prefix).</param>
    /// <param name="policyDocument">The policy document as an object to be serialized to JSON.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>True if the bucket policy was set successfully; otherwise, false.</returns>
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

    /// <summary>
    /// Configures the specified S3 bucket to host a static website.
    /// </summary>
    /// <param name="bucketName">Name of the bucket to configure (without the resource prefix).</param>
    /// <param name="indexDocumentSuffix">The suffix for the index document. Default is "index.html".</param>
    /// <param name="errorDocument">The error document to use. Default is "error.html".</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>True if the website configuration was set successfully; otherwise, false.</returns>
    public async Task<bool> SetBucketWebsiteConfigurationAsync(
        string bucketName,
        string indexDocumentSuffix = "index.html",
        string errorDocument = "error.html",
        CancellationToken cancellationToken = default)
    {
        var bucketNameWithPrefix = $"{options.ResourcePrefix}.{bucketName}";
        logger.LogInformation("Setting website configuration for bucket: {BucketName}", bucketNameWithPrefix);

        var putBucketWebsiteRequest = new PutBucketWebsiteRequest
        {
            BucketName = bucketNameWithPrefix,
            WebsiteConfiguration = new WebsiteConfiguration
            {
                IndexDocumentSuffix = indexDocumentSuffix,
                ErrorDocument = errorDocument
            }
        };

        var response = await amazonS3.PutBucketWebsiteAsync(
            putBucketWebsiteRequest,
            cancellationToken);
        return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
    }

    /// <summary>
    /// Sets the Access Control List (ACL) for the specified S3 bucket.
    /// </summary>
    /// <param name="bucketName">Name of the bucket to configure (without the resource prefix).</param>
    /// <param name="acl">The canned ACL to apply to the bucket (e.g., PublicRead, Private).</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>True if the ACL was set successfully; otherwise, false.</returns>
    public async Task<bool> SetBucketAcl(
        string bucketName,
        S3CannedACL acl,
        CancellationToken cancellationToken = default)
    {
        var bucketNameWithPrefix = $"{options.ResourcePrefix}.{bucketName}";
        logger.LogInformation("Setting ACL for bucket: {BucketName}", bucketNameWithPrefix);

        var putBucketAclRequest = new PutBucketAclRequest
        {
            BucketName = bucketNameWithPrefix,
            ACL = acl
        };

        var response = await amazonS3.PutBucketAclAsync(
            putBucketAclRequest,
            cancellationToken);
        return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
    }
}
