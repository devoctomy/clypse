using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Model.Internal.MarshallTransformations;
using Amazon.S3.Transfer;
using clypse.portal.setup.Services.Upload;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace clypse.portal.setup.Services.S3;

/// <inheritdoc cref="IS3Service" />
public class S3Service(
    IAmazonS3 amazonS3,
    IDirectoryUploadService directoryUploadService,
    SetupOptions options,
    ILogger<S3Service> logger) : IS3Service
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        WriteIndented = false,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };

    /// <inheritdoc />
    public async Task<bool> DoesBucketExistAsync(
        string bucketName,
        CancellationToken cancellationToken = default)
    {
        var bucketNameWithPrefix = $"{options.ResourcePrefix}.{bucketName}";
        logger.LogInformation("Checking if bucket exists: {BucketName}", bucketNameWithPrefix);

        var listBucketsResponse = await amazonS3.ListBucketsAsync(cancellationToken);
        if(listBucketsResponse.HttpStatusCode != System.Net.HttpStatusCode.OK)
        {
            logger.LogError("Failed to list S3 buckets. HTTP Status Code: {StatusCode}", listBucketsResponse.HttpStatusCode);
            return false;
        }

        var bucket = listBucketsResponse.Buckets?.FirstOrDefault(b => b.BucketName.Equals(bucketNameWithPrefix, StringComparison.OrdinalIgnoreCase));
        return bucket != null;
    }

    /// <inheritdoc />
    public async Task<bool> CreateBucketAsync(
        string bucketName,
        bool disableBlockPublicAccess,
        CancellationToken cancellationToken = default)
    {
        var bucketNameWithPrefix = $"{options.ResourcePrefix}.{bucketName}";
        logger.LogInformation("Creating S3 bucket: {BucketName}", bucketNameWithPrefix);

        var putBucketRequest = new PutBucketRequest
        {
            BucketName = bucketNameWithPrefix
        };

        var putBucketResponse = await amazonS3.PutBucketAsync(putBucketRequest, cancellationToken);

        if(disableBlockPublicAccess)
        {
            logger.LogInformation("Enabling public access for bucket.");
            var putPublicAccessBlockRespose = await amazonS3.PutPublicAccessBlockAsync(
                new PutPublicAccessBlockRequest
                {
                    BucketName = bucketNameWithPrefix,
                    PublicAccessBlockConfiguration = new PublicAccessBlockConfiguration
                    {
                        BlockPublicAcls = false,
                        IgnorePublicAcls = false,
                        BlockPublicPolicy = false,
                        RestrictPublicBuckets = false
                    }
                },
                cancellationToken);

            return putPublicAccessBlockRespose.HttpStatusCode == System.Net.HttpStatusCode.OK;
        }

        return putBucketResponse.HttpStatusCode == System.Net.HttpStatusCode.OK;
    }

    /// <inheritdoc />
    public async Task<bool> SetBucketTags(
        string bucketName,
        Dictionary<string, string> tags,
        CancellationToken cancellationToken = default)
    {
        var bucketNameWithPrefix = $"{options.ResourcePrefix}.{bucketName}";
        logger.LogInformation("Setting tags for S3 bucket: {BucketName}", bucketNameWithPrefix);

        var tagSet = tags
            .Select(kv => new Tag { Key = kv.Key, Value = kv.Value })
            .ToList();
        var putBucketTaggingRequest = new PutBucketTaggingRequest
        {
            BucketName = bucketNameWithPrefix,
            TagSet = tagSet
        };

        var putBucketTaggingResponse = await amazonS3.PutBucketTaggingAsync(putBucketTaggingRequest, cancellationToken);
        return putBucketTaggingResponse.HttpStatusCode == System.Net.HttpStatusCode.NoContent;
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

    /// <inheritdoc />
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
        return response.HttpStatusCode == System.Net.HttpStatusCode.NoContent;
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
    public async Task<bool> UploadDirectoryToBucket(
        string bucketName,
        string directoryPath,
        CancellationToken cancellationToken = default)
    {
        var bucketNameWithPrefix = $"{options.ResourcePrefix}.{bucketName}";
        logger.LogInformation("Uploading directory {DirectoryPath} to bucket: {BucketName}", directoryPath, bucketNameWithPrefix);

        if (!Directory.Exists(directoryPath))
        {
            logger.LogError("Directory does not exist: {DirectoryPath}", directoryPath);
            return false;
        }

        try
        {
            await directoryUploadService.UploadDirectoryAsync(
                bucketNameWithPrefix,
                directoryPath,
                cancellationToken);
            logger.LogInformation("Successfully uploaded directory to bucket: {BucketName}", bucketNameWithPrefix);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to upload directory {DirectoryPath} to bucket {BucketName}", directoryPath, bucketNameWithPrefix);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<byte[]> DownloadObjectDataAsync(
        string bucketName,
        string objectKey,
        CancellationToken cancellationToken = default)
    {
        var bucketNameWithPrefix = $"{options.ResourcePrefix}.{bucketName}";
        logger.LogInformation("Downloading {objectKey} from bucket {bucketName}", objectKey, bucketNameWithPrefix);

        using var responseStream = await amazonS3.GetObjectStreamAsync(
            bucketNameWithPrefix,
            objectKey,
            new Dictionary<string, object>(),
            cancellationToken);

        using var dataStream = new MemoryStream();
        await responseStream.CopyToAsync(dataStream, cancellationToken);
        await dataStream.FlushAsync(cancellationToken);

        return dataStream.ToArray();
    }
}
