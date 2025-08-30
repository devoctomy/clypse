using Amazon.S3;
using Amazon.S3.Model;
using clypse.core.Blazor;
using System.Text.Json;

namespace clypse.core.Cloud.Aws.S3;

/// <summary>
/// JavaScript interop implementation of IAmazonS3Client for Blazor WebAssembly compatibility.
/// This client delegates S3 operations to JavaScript AWS SDK calls to bypass .NET platform limitations.
/// </summary>
public class JavaScriptS3Client : IAmazonS3Client
{
    private readonly IJavaScriptS3Invoker jsInvoker;
    private readonly string accessKey;
    private readonly string secretKey;
    private readonly string sessionToken;
    private readonly string region;

    /// <summary>
    /// Initializes a new instance of the <see cref="JavaScriptS3Client"/> class with AWS credentials.
    /// </summary>
    /// <param name="jsInvoker">The JavaScript S3 invoker for interop calls.</param>
    /// <param name="accessKey">AWS access key ID.</param>
    /// <param name="secretKey">AWS secret access key.</param>
    /// <param name="sessionToken">AWS session token (for temporary credentials).</param>
    /// <param name="region">AWS region name.</param>
    public JavaScriptS3Client(
        IJavaScriptS3Invoker jsInvoker,
        string accessKey,
        string secretKey,
        string sessionToken,
        string region)
    {
        this.jsInvoker = jsInvoker;
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
            request.Key,
            AccessKeyId = this.accessKey,
            SecretAccessKey = this.secretKey,
            SessionToken = this.sessionToken,
            Region = this.region,
        };

        var result = await this.jsInvoker.InvokeS3OperationAsync("S3Client.deleteObject", jsRequest);

        if (!result.Success)
        {
            throw new AmazonS3Exception(result.ErrorMessage);
        }

        return new DeleteObjectResponse
        {
            DeleteMarker = JavaScriptInteropUtility.GetStringValue(result.Data, "DeleteMarker", string.Empty) ?? string.Empty,
            VersionId = JavaScriptInteropUtility.GetStringValue(result.Data, "VersionId", string.Empty),
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

        var result = await this.jsInvoker.InvokeS3OperationAsync("S3Client.getObject", jsRequest);

        if (!result.Success)
        {
            throw new AmazonS3Exception(result.ErrorMessage);
        }

        var response = new GetObjectResponse
        {
            BucketName = request.BucketName,
            Key = request.Key,
            ContentLength = JavaScriptInteropUtility.GetLongValue(result.Data, "ContentLength"),
            ETag = JavaScriptInteropUtility.GetStringValue(result.Data, "ETag", string.Empty) ?? string.Empty,
            LastModified = DateTime.Parse(JavaScriptInteropUtility.GetStringValue(result.Data, "LastModified", DateTime.UtcNow.ToString()) ?? DateTime.UtcNow.ToString()),
        };

        var base64Content = JavaScriptInteropUtility.GetStringValue(result.Data, "Body", string.Empty) ?? string.Empty;
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
            request.Key,
            AccessKeyId = this.accessKey,
            SecretAccessKey = this.secretKey,
            SessionToken = this.sessionToken,
            Region = this.region,
        };

        var result = await this.jsInvoker.InvokeS3OperationAsync("S3Client.getObjectMetadata", jsRequest);

        if (!result.Success)
        {
            throw new AmazonS3Exception(result.ErrorMessage);
        }

        return new GetObjectMetadataResponse
        {
            ContentLength = JavaScriptInteropUtility.GetLongValue(result.Data, "ContentLength"),
            ContentType = JavaScriptInteropUtility.GetStringValue(result.Data, "ContentType", string.Empty) ?? string.Empty,
            ETag = JavaScriptInteropUtility.GetStringValue(result.Data, "ETag", string.Empty) ?? string.Empty,
            LastModified = DateTime.Parse(JavaScriptInteropUtility.GetStringValue(result.Data, "LastModified", DateTime.UtcNow.ToString()) ?? DateTime.UtcNow.ToString()),
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

        var result = await this.jsInvoker.InvokeS3OperationAsync("S3Client.listObjectsV2", jsRequest);

        if (!result.Success)
        {
            throw new AmazonS3Exception(result.ErrorMessage);
        }

        var response = new ListObjectsV2Response
        {
            Prefix = request.Prefix,
            IsTruncated = JavaScriptInteropUtility.GetBoolValue(result.Data, "IsTruncated"),
            NextContinuationToken = JavaScriptInteropUtility.GetStringValue(result.Data, "NextContinuationToken", string.Empty),
            S3Objects = [],
            MaxKeys = jsRequest.MaxKeys,
            KeyCount = JavaScriptInteropUtility.GetIntValue(result.Data, "KeyCount"),
        };

        if (result.Data?.ContainsKey("Contents") == true)
        {
            var contentsValue = result.Data.GetValueOrDefault("Contents", null);
            if (contentsValue is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in jsonElement.EnumerateArray())
                {
                    var itemDict = JsonSerializer.Deserialize<Dictionary<string, object?>>(item.GetRawText()) ?? new Dictionary<string, object?>();
                    var lastModifiedString = JavaScriptInteropUtility.GetStringValue(itemDict, "LastModified");
                    var lastModified = (DateTime?)(lastModifiedString != null ? DateTime.Parse(lastModifiedString) : null);
                    response.S3Objects.Add(new S3Object
                    {
                        BucketName = request.BucketName,
                        Key = JavaScriptInteropUtility.GetStringValue(itemDict, "Key", string.Empty) ?? string.Empty,
                        Size = JavaScriptInteropUtility.GetLongValue(itemDict, "Size"),
                        LastModified = lastModified,
                        ETag = JavaScriptInteropUtility.GetStringValue(itemDict, "ETag", string.Empty) ?? string.Empty,
                    });
                }
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
            await request.InputStream.CopyToAsync(memoryStream, cancellationToken);
            bodyData = memoryStream.ToArray();
        }

        if (bodyData == null ||
            bodyData.Length == 0)
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

        var result = await this.jsInvoker.InvokeS3OperationAsync("S3Client.putObject", jsRequest);

        if (!result.Success)
        {
            throw new AmazonS3Exception(result.ErrorMessage);
        }

        return new PutObjectResponse
        {
            ETag = JavaScriptInteropUtility.GetStringValue(result.Data, "ETag", string.Empty) ?? string.Empty,
            VersionId = JavaScriptInteropUtility.GetStringValue(result.Data, "VersionId", string.Empty),
        };
    }
}
