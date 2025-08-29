using System.Text;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.JSInterop;

namespace clypse.core.Cloud.Aws.S3;

/// <summary>
/// JavaScript interop implementation of IAmazonS3Client for Blazor WebAssembly compatibility.
/// This client delegates S3 operations to JavaScript AWS SDK calls to bypass .NET platform limitations.
/// </summary>
public class JavaScriptS3Client : IAmazonS3Client
{
    private readonly IJSRuntime jsRuntime;
    private readonly string accessKey;
    private readonly string secretKey;
    private readonly string sessionToken;
    private readonly string region;

    /// <summary>
    /// Initializes a new instance of the <see cref="JavaScriptS3Client"/> class with AWS credentials.
    /// </summary>
    /// <param name="jsRuntime">The JavaScript runtime for interop calls.</param>
    /// <param name="accessKey">AWS access key ID.</param>
    /// <param name="secretKey">AWS secret access key.</param>
    /// <param name="sessionToken">AWS session token (for temporary credentials).</param>
    /// <param name="region">AWS region name.</param>
    public JavaScriptS3Client(
        IJSRuntime jsRuntime,
        string accessKey,
        string secretKey,
        string sessionToken,
        string region)
    {
        this.jsRuntime = jsRuntime;
        this.accessKey = accessKey;
        this.secretKey = secretKey;
        this.sessionToken = sessionToken;
        this.region = region;
    }

    /// <summary>
    /// Deletes an object from Amazon S3.
    /// </summary>
    /// <param name="request">The request containing the bucket name and object key to delete.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A response indicating the result of the delete operation.</returns>
    public async Task<DeleteObjectResponse> DeleteObjectAsync(
        DeleteObjectRequest request,
        CancellationToken cancellationToken)
    {
        var jsRequest = new
        {
            Bucket = request.BucketName,
            Key = request.Key,
            AccessKeyId = this.accessKey,
            SecretAccessKey = this.secretKey,
            SessionToken = this.sessionToken,
            Region = this.region,
        };

        var result = await this.jsRuntime.InvokeAsync<S3OperationResult>("S3Client.deleteObject", jsRequest);

        if (!result.Success)
        {
            throw new AmazonS3Exception(result.ErrorMessage);
        }

        return new DeleteObjectResponse
        {
            DeleteMarker = result.Data?.GetValueOrDefault("DeleteMarker", string.Empty)?.ToString() ?? string.Empty,
            VersionId = result.Data?.GetValueOrDefault("VersionId", string.Empty)?.ToString(),
        };
    }

    /// <summary>
    /// Retrieves an object from Amazon S3, including both metadata and the object data.
    /// </summary>
    /// <param name="request">The request containing the bucket name and object key to retrieve.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A response containing the object data and metadata.</returns>
    public async Task<GetObjectResponse> GetObjectAsync(
        GetObjectRequest request,
        CancellationToken cancellationToken)
    {
        var jsRequest = new
        {
            Bucket = request.BucketName,
            request.Key,
            AccessKeyId = this.accessKey,
            SecretAccessKey = this.secretKey,
            SessionToken = this.sessionToken,
            Region = this.region,
        };

        var result = await this.jsRuntime.InvokeAsync<S3OperationResult>("S3Client.getObject", jsRequest);

        if (!result.Success)
        {
            throw new AmazonS3Exception(result.ErrorMessage);
        }

        var response = new GetObjectResponse
        {
            BucketName = request.BucketName,
            Key = request.Key,
            ContentLength = (long)(result.Data?.GetValueOrDefault("ContentLength", 0) ?? 0),
            ////ContentType = result.Data?.GetValueOrDefault("ContentType", string.Empty)?.ToString() ?? string.Empty,
            ETag = result.Data?.GetValueOrDefault("ETag", string.Empty)?.ToString() ?? string.Empty,
            LastModified = DateTime.Parse(result.Data?.GetValueOrDefault("LastModified", DateTime.UtcNow.ToString())?.ToString() ?? DateTime.UtcNow.ToString()),
        };

        var base64Content = result.Data?.GetValueOrDefault("Body", string.Empty)?.ToString() ?? string.Empty;
        if (!string.IsNullOrEmpty(base64Content))
        {
            var bytes = Convert.FromBase64String(base64Content);
            response.ResponseStream = new MemoryStream(bytes);
        }

        return response;
    }

    /// <summary>
    /// Retrieves metadata for an object stored in Amazon S3 without downloading the object itself.
    /// </summary>
    /// <param name="request">The request containing the bucket name and object key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A response containing the object metadata.</returns>
    public async Task<GetObjectMetadataResponse> GetObjectMetadataAsync(
        GetObjectMetadataRequest request,
        CancellationToken cancellationToken)
    {
        var jsRequest = new
        {
            Bucket = request.BucketName,
            Key = request.Key,
            AccessKeyId = this.accessKey,
            SecretAccessKey = this.secretKey,
            SessionToken = this.sessionToken,
            Region = this.region,
        };

        var result = await this.jsRuntime.InvokeAsync<S3OperationResult>("S3Client.getObjectMetadata", jsRequest);

        if (!result.Success)
        {
            throw new AmazonS3Exception(result.ErrorMessage);
        }

        return new GetObjectMetadataResponse
        {
            ContentLength = (long)(result.Data?.GetValueOrDefault("ContentLength", 0) ?? 0),
            ContentType = result.Data?.GetValueOrDefault("ContentType", string.Empty)?.ToString() ?? string.Empty,
            ETag = result.Data?.GetValueOrDefault("ETag", string.Empty)?.ToString() ?? string.Empty,
            LastModified = DateTime.Parse(result.Data?.GetValueOrDefault("LastModified", DateTime.UtcNow.ToString())?.ToString() ?? DateTime.UtcNow.ToString()),
        };
    }

    /// <summary>
    /// Lists objects in an Amazon S3 bucket using the ListObjectsV2 API.
    /// </summary>
    /// <param name="request">The request containing the bucket name and optional filtering parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A response containing the list of objects that match the request criteria.</returns>
    public async Task<ListObjectsV2Response> ListObjectsV2Async(
        ListObjectsV2Request request,
        CancellationToken cancellationToken)
    {
        var jsRequest = new
        {
            Bucket = request.BucketName,
            request.Prefix,
            request.MaxKeys,
            request.ContinuationToken,
            AccessKeyId = this.accessKey,
            SecretAccessKey = this.secretKey,
            SessionToken = this.sessionToken,
            Region = this.region,
        };

        var result = await this.jsRuntime.InvokeAsync<S3OperationResult>("S3Client.listObjectsV2", jsRequest);

        if (!result.Success)
        {
            throw new AmazonS3Exception(result.ErrorMessage);
        }

        var response = new ListObjectsV2Response
        {
            Prefix = request.Prefix,
            IsTruncated = (bool)(result.Data?.GetValueOrDefault("IsTruncated", false) ?? false),
            NextContinuationToken = result.Data?.GetValueOrDefault("NextContinuationToken", string.Empty)?.ToString(),
            S3Objects = new List<S3Object>(),
        };

        if (result.Data?.ContainsKey("Contents") == true && result.Data["Contents"] is JsonElement contentsElement)
        {
            foreach (var obj in contentsElement.EnumerateArray())
            {
                response.S3Objects.Add(new S3Object
                {
                    BucketName = request.BucketName,
                    Key = obj.GetProperty("Key").GetString() ?? string.Empty,
                    Size = obj.GetProperty("Size").GetInt64(),
                    LastModified = DateTime.Parse(obj.GetProperty("LastModified").GetString() ?? DateTime.UtcNow.ToString()),
                    ETag = obj.GetProperty("ETag").GetString() ?? string.Empty,
                });
            }
        }

        return response;
    }

    /// <summary>
    /// Stores an object in Amazon S3.
    /// </summary>
    /// <param name="request">The request containing the bucket name, object key, and data to store.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A response indicating the result of the put operation.</returns>
    public async Task<PutObjectResponse> PutObjectAsync(
        PutObjectRequest request,
        CancellationToken cancellationToken)
    {
        byte[]? bodyData = null;
        if (request.InputStream != null)
        {
            using var memoryStream = new MemoryStream();
            await request.InputStream.CopyToAsync(memoryStream);
            bodyData = memoryStream.ToArray();
        }
        else if (!string.IsNullOrEmpty(request.ContentBody))
        {
            bodyData = Encoding.UTF8.GetBytes(request.ContentBody);
        }

        // Debug: check if bodyData is null or empty
        if (bodyData == null || bodyData.Length == 0)
        {
            throw new AmazonS3Exception($"No body data available. InputStream: {request.InputStream != null}, ContentBody: {!string.IsNullOrEmpty(request.ContentBody)}");
        }

        var jsRequest = new
        {
            Bucket = request.BucketName,
            request.Key,
            Body = bodyData,
            ContentType = "application/octet-stream",
            AccessKeyId = this.accessKey,
            SecretAccessKey = this.secretKey,
            SessionToken = this.sessionToken,
            Region = this.region,
        };

        var result = await this.jsRuntime.InvokeAsync<S3OperationResult>("S3Client.putObject", jsRequest);

        if (!result.Success)
        {
            throw new AmazonS3Exception(result.ErrorMessage);
        }

        return new PutObjectResponse
        {
            ETag = result.Data?.GetValueOrDefault("ETag", string.Empty)?.ToString() ?? string.Empty,
            VersionId = result.Data?.GetValueOrDefault("VersionId", string.Empty)?.ToString(),
        };
    }

    /// <summary>
    /// Result structure for JavaScript S3 operations.
    /// </summary>
    private class S3OperationResult
    {
        public bool Success { get; set; }

        public string ErrorMessage { get; set; } = string.Empty;

        public Dictionary<string, object>? Data { get; set; }
    }
}
