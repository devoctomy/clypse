using System.Text;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using clypse.core.Cloud.Aws.S3;
using Moq;

namespace clypse.core.UnitTests.Cloud.Aws.S3;

public class JavaScriptS3ClientTests
{
    private readonly Mock<IJavaScriptS3Invoker> mockJsInvoker;
    private readonly string testAccessKey = "test-access-key";
    private readonly string testSecretKey = "test-secret-key";
    private readonly string testSessionToken = "test-session-token";
    private readonly string testRegion = "us-west-2";
    private readonly string testBucket = "test-bucket";
    private readonly string testKey = "test-key";

    public JavaScriptS3ClientTests()
    {
        this.mockJsInvoker = new Mock<IJavaScriptS3Invoker>();
    }

    [Fact]
    public void GivenValidParameters_WhenConstructingJavaScriptS3Client_ThenClientCreatedSuccessfully()
    {
        // Arrange & Act
        var sut = new JavaScriptS3Client(
            this.mockJsInvoker.Object,
            this.testAccessKey,
            this.testSecretKey,
            this.testSessionToken,
            this.testRegion);

        // Assert
        Assert.NotNull(sut);
    }

    [Fact]
    public async Task GivenValidRequest_WhenPutObjectAsync_ThenSuccessfulResponseReturned()
    {
        // Arrange
        var sut = new JavaScriptS3Client(
            this.mockJsInvoker.Object,
            this.testAccessKey,
            this.testSecretKey,
            this.testSessionToken,
            this.testRegion);

        var testData = Encoding.UTF8.GetBytes("Test Data");
        var request = new PutObjectRequest
        {
            BucketName = this.testBucket,
            Key = this.testKey,
            InputStream = new MemoryStream(testData),
        };

        var expectedETag = "\"d41d8cd98f00b204e9800998ecf8427e\"";
        var expectedVersionId = "version123";

        var jsResult = new
        {
            Success = true,
            ErrorMessage = string.Empty,
            Data = new Dictionary<string, object>
            {
                { "ETag", expectedETag },
                { "VersionId", expectedVersionId },
            },
        };

        this.mockJsInvoker
            .Setup(x => x.InvokeS3OperationAsync("S3Client.putObject", It.IsAny<object>()))
            .ReturnsAsync(JsonSerializer.Deserialize<S3OperationResult>(JsonSerializer.Serialize(jsResult)) !);

        // Act
        var response = await sut.PutObjectAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(expectedETag, response.ETag);
        Assert.Equal(expectedVersionId, response.VersionId);

        this.mockJsInvoker.Verify(
            x => x.InvokeS3OperationAsync("S3Client.putObject", It.Is<object>(req =>
                this.VerifyPutObjectRequest(req, this.testBucket, this.testKey, testData))),
            Times.Once);
    }

    [Fact]
    public async Task GivenJavaScriptError_WhenPutObjectAsync_ThenAmazonS3ExceptionThrown()
    {
        // Arrange
        var sut = new JavaScriptS3Client(
            this.mockJsInvoker.Object,
            this.testAccessKey,
            this.testSecretKey,
            this.testSessionToken,
            this.testRegion);

        var request = new PutObjectRequest
        {
            BucketName = this.testBucket,
            Key = this.testKey,
            InputStream = new MemoryStream(Encoding.UTF8.GetBytes("test")),
        };

        var jsResult = new
        {
            Success = false,
            ErrorMessage = "Access denied",
            Data = (Dictionary<string, object>?)null,
        };

        this.mockJsInvoker
            .Setup(x => x.InvokeS3OperationAsync("S3Client.putObject", It.IsAny<object>()))
            .ReturnsAsync(JsonSerializer.Deserialize<S3OperationResult>(JsonSerializer.Serialize(jsResult)) !);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AmazonS3Exception>(
            () => sut.PutObjectAsync(request, CancellationToken.None));

        Assert.Equal("Access denied", exception.Message);
    }

    [Fact]
    public async Task GivenNullInputStream_WhenPutObjectAsync_ThenAmazonS3ExceptionThrown()
    {
        // Arrange
        var sut = new JavaScriptS3Client(
            this.mockJsInvoker.Object,
            this.testAccessKey,
            this.testSecretKey,
            this.testSessionToken,
            this.testRegion);

        var request = new PutObjectRequest
        {
            BucketName = this.testBucket,
            Key = this.testKey,
            InputStream = null,
        };

        var jsResult = new
        {
            Success = true,
            ErrorMessage = string.Empty,
            Data = new Dictionary<string, object>
            {
                { "ETag", "test-etag" },
            },
        };

        this.mockJsInvoker
            .Setup(x => x.InvokeS3OperationAsync("S3Client.putObject", It.IsAny<object>()))
            .ReturnsAsync(JsonSerializer.Deserialize<S3OperationResult>(JsonSerializer.Serialize(jsResult)) !);

        // Act & Assert
        await Assert.ThrowsAnyAsync<AmazonS3Exception>(async () =>
        {
            _ = await sut.PutObjectAsync(request, CancellationToken.None);
        });
    }

    [Fact]
    public async Task GivenValidRequest_WhenGetObjectAsync_ThenSuccessfulResponseReturned()
    {
        // Arrange
        var sut = new JavaScriptS3Client(
            this.mockJsInvoker.Object,
            this.testAccessKey,
            this.testSecretKey,
            this.testSessionToken,
            this.testRegion);

        var request = new GetObjectRequest
        {
            BucketName = this.testBucket,
            Key = this.testKey,
        };

        var testData = Encoding.UTF8.GetBytes("test response data");
        var base64Data = Convert.ToBase64String(testData);
        var expectedETag = "\"test-etag\"";
        var expectedLastModified = DateTime.UtcNow;

        var jsResult = new S3OperationResult
        {
            Success = true,
            ErrorMessage = string.Empty,
            Data = new Dictionary<string, object?>
            {
                { "Body", base64Data },
                { "ContentLength", (long)testData.Length },
                { "ETag", expectedETag },
                { "LastModified", expectedLastModified.ToString() },
            },
        };

        this.mockJsInvoker
            .Setup(x => x.InvokeS3OperationAsync("S3Client.getObject", It.IsAny<object>()))
            .ReturnsAsync(jsResult);

        // Act
        var response = await sut.GetObjectAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(this.testBucket, response.BucketName);
        Assert.Equal(this.testKey, response.Key);
        Assert.Equal(testData.Length, response.ContentLength);
        Assert.Equal(expectedETag, response.ETag);
        Assert.NotNull(response.ResponseStream);

        var responseData = new byte[response.ResponseStream.Length];
        response.ResponseStream.Position = 0;
        await response.ResponseStream.ReadAsync(responseData);
        Assert.Equal(testData, responseData);

        this.mockJsInvoker.Verify(
            x => x.InvokeS3OperationAsync("S3Client.getObject", It.Is<object>(req =>
                this.VerifyGetObjectRequest(req, this.testBucket, this.testKey))),
            Times.Once);
    }

    [Fact]
    public async Task GivenJavaScriptError_WhenGetObjectAsync_ThenAmazonS3ExceptionThrown()
    {
        // Arrange
        var sut = new JavaScriptS3Client(
            this.mockJsInvoker.Object,
            this.testAccessKey,
            this.testSecretKey,
            this.testSessionToken,
            this.testRegion);

        var request = new GetObjectRequest
        {
            BucketName = this.testBucket,
            Key = this.testKey,
        };

        var jsResult = new
        {
            Success = false,
            ErrorMessage = "Object not found",
            Data = (Dictionary<string, object>?)null,
        };

        this.mockJsInvoker
            .Setup(x => x.InvokeS3OperationAsync("S3Client.getObject", It.IsAny<object>()))
            .ReturnsAsync(JsonSerializer.Deserialize<S3OperationResult>(JsonSerializer.Serialize(jsResult)) !);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AmazonS3Exception>(
            () => sut.GetObjectAsync(request, CancellationToken.None));

        Assert.Equal("Object not found", exception.Message);
    }

    [Fact]
    public async Task GivenEmptyBodyInResponse_WhenGetObjectAsync_ThenResponseStreamIsEmpty()
    {
        // Arrange
        var sut = new JavaScriptS3Client(
            this.mockJsInvoker.Object,
            this.testAccessKey,
            this.testSecretKey,
            this.testSessionToken,
            this.testRegion);

        var request = new GetObjectRequest
        {
            BucketName = this.testBucket,
            Key = this.testKey,
        };

        var jsResult = new S3OperationResult
        {
            Success = true,
            ErrorMessage = string.Empty,
            Data = new Dictionary<string, object?>
            {
                { "Body", string.Empty },
                { "ContentLength", 0L },
                { "ETag", "test-etag" },
                { "LastModified", DateTime.UtcNow.ToString() },
            },
        };

        this.mockJsInvoker
            .Setup(x => x.InvokeS3OperationAsync("S3Client.getObject", It.IsAny<object>()))
            .ReturnsAsync(jsResult);

        // Act
        var response = await sut.GetObjectAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(0, response.ContentLength);
        Assert.Null(response.ResponseStream);
    }

    [Fact]
    public async Task GivenValidRequest_WhenDeleteObjectAsync_ThenSuccessfulResponseReturned()
    {
        // Arrange
        var sut = new JavaScriptS3Client(
            this.mockJsInvoker.Object,
            this.testAccessKey,
            this.testSecretKey,
            this.testSessionToken,
            this.testRegion);

        var request = new DeleteObjectRequest
        {
            BucketName = this.testBucket,
            Key = this.testKey,
        };

        var jsResult = new
        {
            Success = true,
            ErrorMessage = string.Empty,
            Data = new Dictionary<string, object>(),
        };

        this.mockJsInvoker
            .Setup(x => x.InvokeS3OperationAsync("S3Client.deleteObject", It.IsAny<object>()))
            .ReturnsAsync(JsonSerializer.Deserialize<S3OperationResult>(JsonSerializer.Serialize(jsResult)) !);

        // Act
        var response = await sut.DeleteObjectAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(response);

        this.mockJsInvoker.Verify(
            x => x.InvokeS3OperationAsync("S3Client.deleteObject", It.Is<object>(req =>
                this.VerifyDeleteObjectRequest(req, this.testBucket, this.testKey))),
            Times.Once);
    }

    [Fact]
    public async Task GivenJavaScriptError_WhenDeleteObjectAsync_ThenAmazonS3ExceptionThrown()
    {
        // Arrange
        var sut = new JavaScriptS3Client(
            this.mockJsInvoker.Object,
            this.testAccessKey,
            this.testSecretKey,
            this.testSessionToken,
            this.testRegion);

        var request = new DeleteObjectRequest
        {
            BucketName = this.testBucket,
            Key = this.testKey,
        };

        var jsResult = new
        {
            Success = false,
            ErrorMessage = "Insufficient permissions",
            Data = (Dictionary<string, object>?)null,
        };

        this.mockJsInvoker
            .Setup(x => x.InvokeS3OperationAsync("S3Client.deleteObject", It.IsAny<object>()))
            .ReturnsAsync(JsonSerializer.Deserialize<S3OperationResult>(JsonSerializer.Serialize(jsResult)) !);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AmazonS3Exception>(
            () => sut.DeleteObjectAsync(request, CancellationToken.None));

        Assert.Equal("Insufficient permissions", exception.Message);
    }

    [Fact]
    public async Task GivenValidRequest_WhenGetObjectMetadataAsync_ThenSuccessfulResponseReturned()
    {
        // Arrange
        var sut = new JavaScriptS3Client(
            this.mockJsInvoker.Object,
            this.testAccessKey,
            this.testSecretKey,
            this.testSessionToken,
            this.testRegion);

        var request = new GetObjectMetadataRequest
        {
            BucketName = this.testBucket,
            Key = this.testKey,
        };

        var expectedETag = "\"test-etag\"";
        var expectedLastModified = DateTime.UtcNow;
        var expectedContentLength = 1024L;

        var jsResult = new S3OperationResult
        {
            Success = true,
            ErrorMessage = string.Empty,
            Data = new Dictionary<string, object?>
            {
                { "ETag", expectedETag },
                { "LastModified", expectedLastModified.ToString() },
                { "ContentLength", expectedContentLength },
            },
        };

        this.mockJsInvoker
            .Setup(x => x.InvokeS3OperationAsync("S3Client.getObjectMetadata", It.IsAny<object>()))
            .ReturnsAsync(jsResult);

        // Act
        var response = await sut.GetObjectMetadataAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(expectedETag, response.ETag);
        Assert.Equal(expectedContentLength, response.ContentLength);

        this.mockJsInvoker.Verify(
            x => x.InvokeS3OperationAsync("S3Client.getObjectMetadata", It.Is<object>(req =>
                this.VerifyGetObjectMetadataRequest(req, this.testBucket, this.testKey))),
            Times.Once);
    }

    [Fact]
    public async Task GivenJavaScriptError_WhenGetObjectMetadataAsync_ThenAmazonS3ExceptionThrown()
    {
        // Arrange
        var sut = new JavaScriptS3Client(
            this.mockJsInvoker.Object,
            this.testAccessKey,
            this.testSecretKey,
            this.testSessionToken,
            this.testRegion);

        var request = new GetObjectMetadataRequest
        {
            BucketName = this.testBucket,
            Key = this.testKey,
        };

        var jsResult = new
        {
            Success = false,
            ErrorMessage = "Metadata not available",
            Data = (Dictionary<string, object>?)null,
        };

        this.mockJsInvoker
            .Setup(x => x.InvokeS3OperationAsync("S3Client.getObjectMetadata", It.IsAny<object>()))
            .ReturnsAsync(JsonSerializer.Deserialize<S3OperationResult>(JsonSerializer.Serialize(jsResult)) !);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AmazonS3Exception>(
            () => sut.GetObjectMetadataAsync(request, CancellationToken.None));

        Assert.Equal("Metadata not available", exception.Message);
    }

    [Fact]
    public async Task GivenValidRequest_WhenListObjectsV2Async_ThenSuccessfulResponseReturned()
    {
        // Arrange
        var sut = new JavaScriptS3Client(
            this.mockJsInvoker.Object,
            this.testAccessKey,
            this.testSecretKey,
            this.testSessionToken,
            this.testRegion);

        var request = new ListObjectsV2Request
        {
            BucketName = this.testBucket,
            Prefix = "test-prefix/",
            MaxKeys = 10,
        };

        var jsResult = new S3OperationResult
        {
            Success = true,
            ErrorMessage = string.Empty,
            Data = new Dictionary<string, object?>
            {
                { "IsTruncated", false },
                { "MaxKeys", 10 },
                { "KeyCount", 2 },
                {
                    "Contents", JsonSerializer.Deserialize<JsonElement>(@"[
                        {
                            ""Key"": ""test-prefix/file1.txt"",
                            ""Size"": 100,
                            ""ETag"": ""\""etag1\"""",
                            ""LastModified"": """ + DateTime.UtcNow.ToString() + @"""
                        },
                        {
                            ""Key"": ""test-prefix/file2.txt"",
                            ""Size"": 200,
                            ""ETag"": ""\""etag2\"""",
                            ""LastModified"": """ + DateTime.UtcNow.ToString() + @"""
                        }
                    ]")
                },
            },
        };

        this.mockJsInvoker
            .Setup(x => x.InvokeS3OperationAsync("S3Client.listObjectsV2", It.IsAny<object>()))
            .ReturnsAsync(jsResult!);

        // Act
        var response = await sut.ListObjectsV2Async(request, CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.False(response.IsTruncated);
        Assert.Equal(10, response.MaxKeys);
        Assert.Equal(2, response.KeyCount);
        Assert.Equal(2, response.S3Objects.Count);

        var firstObject = response.S3Objects[0];
        Assert.Equal("test-prefix/file1.txt", firstObject.Key);
        Assert.Equal(100, firstObject.Size);
        Assert.Equal("\"etag1\"", firstObject.ETag);

        this.mockJsInvoker.Verify(
            x => x.InvokeS3OperationAsync("S3Client.listObjectsV2", It.Is<object>(req =>
                this.VerifyListObjectsV2Request(req, this.testBucket, "test-prefix/", 10))),
            Times.Once);
    }

    [Fact]
    public async Task GivenJavaScriptError_WhenListObjectsV2Async_ThenAmazonS3ExceptionThrown()
    {
        // Arrange
        var sut = new JavaScriptS3Client(
            this.mockJsInvoker.Object,
            this.testAccessKey,
            this.testSecretKey,
            this.testSessionToken,
            this.testRegion);

        var request = new ListObjectsV2Request
        {
            BucketName = this.testBucket,
        };

        var jsResult = new
        {
            Success = false,
            ErrorMessage = "Bucket access denied",
            Data = (Dictionary<string, object>?)null,
        };

        this.mockJsInvoker
            .Setup(x => x.InvokeS3OperationAsync("S3Client.listObjectsV2", It.IsAny<object>()))
            .ReturnsAsync(JsonSerializer.Deserialize<S3OperationResult>(JsonSerializer.Serialize(jsResult)) !);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AmazonS3Exception>(
            () => sut.ListObjectsV2Async(request, CancellationToken.None));

        Assert.Equal("Bucket access denied", exception.Message);
    }

    private static bool DictionaryContainsAndStringValueEquals(
        Dictionary<string, object>? dictionary,
        string key,
        object expectedValue)
    {
        if (dictionary == null ||
            !dictionary.TryGetValue(key, out var value))
        {
            return false;
        }

        return value.ToString() == expectedValue.ToString();
    }

    private bool VerifyPutObjectRequest(object request, string expectedBucket, string expectedKey, byte[] expectedData)
    {
        var json = JsonSerializer.Serialize(request);
        var requestObj = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

        return DictionaryContainsAndStringValueEquals(requestObj, "Bucket", expectedBucket) &&
               DictionaryContainsAndStringValueEquals(requestObj, "Key", expectedKey) &&
               DictionaryContainsAndStringValueEquals(requestObj, "Body", Convert.ToBase64String(expectedData)) &&
               DictionaryContainsAndStringValueEquals(requestObj, "AccessKeyId", this.testAccessKey) &&
               DictionaryContainsAndStringValueEquals(requestObj, "SecretAccessKey", this.testSecretKey) &&
               DictionaryContainsAndStringValueEquals(requestObj, "SessionToken", this.testSessionToken) &&
               DictionaryContainsAndStringValueEquals(requestObj, "Region", this.testRegion);
    }

    private bool VerifyGetObjectRequest(object request, string expectedBucket, string expectedKey)
    {
        var json = JsonSerializer.Serialize(request);
        var requestObj = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

        return DictionaryContainsAndStringValueEquals(requestObj, "Bucket", expectedBucket) &&
               DictionaryContainsAndStringValueEquals(requestObj, "Key", expectedKey) &&
               DictionaryContainsAndStringValueEquals(requestObj, "AccessKeyId", this.testAccessKey) &&
               DictionaryContainsAndStringValueEquals(requestObj, "SecretAccessKey", this.testSecretKey) &&
               DictionaryContainsAndStringValueEquals(requestObj, "SessionToken", this.testSessionToken) &&
               DictionaryContainsAndStringValueEquals(requestObj, "Region", this.testRegion);
    }

    private bool VerifyDeleteObjectRequest(
        object request,
        string expectedBucket,
        string expectedKey)
    {
        var json = JsonSerializer.Serialize(request);
        var requestObj = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

        return DictionaryContainsAndStringValueEquals(requestObj, "Bucket", expectedBucket) &&
               DictionaryContainsAndStringValueEquals(requestObj, "Key", expectedKey) &&
               DictionaryContainsAndStringValueEquals(requestObj, "AccessKeyId", this.testAccessKey) &&
               DictionaryContainsAndStringValueEquals(requestObj, "SecretAccessKey", this.testSecretKey) &&
               DictionaryContainsAndStringValueEquals(requestObj, "SessionToken", this.testSessionToken) &&
               DictionaryContainsAndStringValueEquals(requestObj, "Region", this.testRegion);
    }

    private bool VerifyGetObjectMetadataRequest(
        object request,
        string expectedBucket,
        string expectedKey)
    {
        var json = JsonSerializer.Serialize(request);
        var requestObj = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

        return DictionaryContainsAndStringValueEquals(requestObj, "Bucket", expectedBucket) &&
               DictionaryContainsAndStringValueEquals(requestObj, "Key", expectedKey) &&
               DictionaryContainsAndStringValueEquals(requestObj, "AccessKeyId", this.testAccessKey) &&
               DictionaryContainsAndStringValueEquals(requestObj, "SecretAccessKey", this.testSecretKey) &&
               DictionaryContainsAndStringValueEquals(requestObj, "SessionToken", this.testSessionToken) &&
               DictionaryContainsAndStringValueEquals(requestObj, "Region", this.testRegion);
    }

    private bool VerifyListObjectsV2Request(
        object request,
        string expectedBucket,
        string expectedPrefix,
        int expectedMaxKeys)
    {
        var json = JsonSerializer.Serialize(request);
        var requestObj = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

        return DictionaryContainsAndStringValueEquals(requestObj, "Bucket", expectedBucket) &&
               DictionaryContainsAndStringValueEquals(requestObj, "Prefix", expectedPrefix) &&
               DictionaryContainsAndStringValueEquals(requestObj, "MaxKeys", expectedMaxKeys) &&
               DictionaryContainsAndStringValueEquals(requestObj, "AccessKeyId", this.testAccessKey) &&
               DictionaryContainsAndStringValueEquals(requestObj, "SecretAccessKey", this.testSecretKey) &&
               DictionaryContainsAndStringValueEquals(requestObj, "SessionToken", this.testSessionToken) &&
               DictionaryContainsAndStringValueEquals(requestObj, "Region", this.testRegion);
    }
}
