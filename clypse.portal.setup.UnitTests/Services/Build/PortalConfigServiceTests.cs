using clypse.portal.setup.Services.Build;
using clypse.portal.setup.Services.IO;
using Moq;
using System.Text;
using System.Text.Json;

namespace clypse.portal.setup.UnitTests.Services.Build;

public class PortalConfigServiceTests
{
    [Fact]
    public async Task GivenValidTemplateAndConfig_WhenConfigureAsync_ThenReturnsConfiguredStream()
    {
        // Arrange
        var mockIoService = new Mock<IIoService>();
        var sut = new PortalConfigService(mockIoService.Object);

        var templateJson = """
            {
              "AwsS3": {
                "BucketName": "placeholder-bucket",
                "Region": "placeholder-region"
              },
              "AwsCognito": {
                "UserPoolId": "placeholder-pool-id",
                "UserPoolClientId": "placeholder-client-id",
                "Region": "placeholder-cognito-region",
                "IdentityPoolId": "placeholder-identity-pool-id"
              }
            }
            """;

        mockIoService
            .Setup(io => io.ReadAllTextAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(templateJson);

        var s3DataBucketName = "my-data-bucket";
        var s3Region = "us-west-2";
        var cognitoUserPoolId = "us-east-1_ABC123";
        var cognitoUserPoolClientId = "client123";
        var cognitoRegion = "us-east-1";
        var cognitoIdentityPoolId = "us-east-1:identity-123";

        // Act
        var result = await sut.ConfigureAsync(
            "template.json",
            s3DataBucketName,
            s3Region,
            cognitoUserPoolId,
            cognitoUserPoolClientId,
            cognitoRegion,
            cognitoIdentityPoolId);

        // Assert
        Assert.NotNull(result);
        result.Seek(0, SeekOrigin.Begin);
        var resultJson = await JsonDocument.ParseAsync(result);
        
        Assert.Equal(s3DataBucketName, resultJson.RootElement.GetProperty("AwsS3").GetProperty("BucketName").GetString());
        Assert.Equal(s3Region, resultJson.RootElement.GetProperty("AwsS3").GetProperty("Region").GetString());
        Assert.Equal(cognitoUserPoolId, resultJson.RootElement.GetProperty("AwsCognito").GetProperty("UserPoolId").GetString());
        Assert.Equal(cognitoUserPoolClientId, resultJson.RootElement.GetProperty("AwsCognito").GetProperty("UserPoolClientId").GetString());
        Assert.Equal(cognitoRegion, resultJson.RootElement.GetProperty("AwsCognito").GetProperty("Region").GetString());
        Assert.Equal(cognitoIdentityPoolId, resultJson.RootElement.GetProperty("AwsCognito").GetProperty("IdentityPoolId").GetString());
        
        mockIoService.Verify(io => io.ReadAllTextAsync(
            "template.json",
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GivenInvalidJson_WhenConfigureAsync_ThenThrowsException()
    {
        // Arrange
        var mockIoService = new Mock<IIoService>();
        var sut = new PortalConfigService(mockIoService.Object);

        var invalidJson = "{ this is not valid json }";

        mockIoService
            .Setup(io => io.ReadAllTextAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(invalidJson);

        // Act & Assert
        // This is done like this because for some weird reason Microsoft haven't made the exception accessible.
        try
        {
            await sut.ConfigureAsync(
                "template.json",
                "bucket",
                "region",
                "pool-id",
                "client-id",
                "region",
                "identity-pool-id");
        }
        catch (Exception ex)
        {
            Assert.Equal("System.Text.Json", ex.Source);
        }
    }

    [Fact]
    public async Task GivenNullJson_WhenConfigureAsync_ThenThrowsException()
    {
        // Arrange
        var mockIoService = new Mock<IIoService>();
        var sut = new PortalConfigService(mockIoService.Object);

        mockIoService
            .Setup(io => io.ReadAllTextAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("null");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => sut.ConfigureAsync(
            "template.json",
            "bucket",
            "region",
            "pool-id",
            "client-id",
            "region",
            "identity-pool-id"));
        
        Assert.Equal("Failed to parse template JSON.", exception.Message);
    }

    [Fact]
    public async Task GivenCancellationToken_WhenConfigureAsync_ThenPassesCancellationToken()
    {
        // Arrange
        var mockIoService = new Mock<IIoService>();
        var sut = new PortalConfigService(mockIoService.Object);
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        var templateJson = """
            {
              "AwsS3": {
                "BucketName": "placeholder-bucket",
                "Region": "placeholder-region"
              },
              "AwsCognito": {
                "UserPoolId": "placeholder-pool-id",
                "UserPoolClientId": "placeholder-client-id",
                "Region": "placeholder-cognito-region",
                "IdentityPoolId": "placeholder-identity-pool-id"
              }
            }
            """;

        mockIoService
            .Setup(io => io.ReadAllTextAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(templateJson);

        // Act
        var result = await sut.ConfigureAsync(
            "template.json",
            "bucket",
            "region",
            "pool-id",
            "client-id",
            "region",
            "identity-pool-id",
            cancellationToken);

        // Assert
        Assert.NotNull(result);
        mockIoService.Verify(io => io.ReadAllTextAsync(
            It.IsAny<string>(),
            cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task GivenValidTemplate_WhenConfigureAsync_ThenOutputStreamIsAtBeginning()
    {
        // Arrange
        var mockIoService = new Mock<IIoService>();
        var sut = new PortalConfigService(mockIoService.Object);

        var templateJson = """
            {
              "AwsS3": {
                "BucketName": "placeholder-bucket",
                "Region": "placeholder-region"
              },
              "AwsCognito": {
                "UserPoolId": "placeholder-pool-id",
                "UserPoolClientId": "placeholder-client-id",
                "Region": "placeholder-cognito-region",
                "IdentityPoolId": "placeholder-identity-pool-id"
              }
            }
            """;

        mockIoService
            .Setup(io => io.ReadAllTextAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(templateJson);

        // Act
        var result = await sut.ConfigureAsync(
            "template.json",
            "bucket",
            "region",
            "pool-id",
            "client-id",
            "region",
            "identity-pool-id");

        // Assert
        Assert.Equal(0, result.Position);
    }

    [Fact]
    public async Task GivenValidTemplate_WhenConfigureAsync_ThenOutputIsValidIndentedJson()
    {
        // Arrange
        var mockIoService = new Mock<IIoService>();
        var sut = new PortalConfigService(mockIoService.Object);

        var templateJson = """
            {
              "AwsS3": {
                "BucketName": "placeholder-bucket",
                "Region": "placeholder-region"
              },
              "AwsCognito": {
                "UserPoolId": "placeholder-pool-id",
                "UserPoolClientId": "placeholder-client-id",
                "Region": "placeholder-cognito-region",
                "IdentityPoolId": "placeholder-identity-pool-id"
              }
            }
            """;

        mockIoService
            .Setup(io => io.ReadAllTextAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(templateJson);

        // Act
        var result = await sut.ConfigureAsync(
            "template.json",
            "bucket",
            "us-west-2",
            "pool-id",
            "client-id",
            "us-east-1",
            "identity-pool-id");

        // Assert
        result.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(result, Encoding.UTF8);
        var outputText = await reader.ReadToEndAsync();
        
        // Verify it's valid JSON
        var doc = JsonDocument.Parse(outputText);
        Assert.NotNull(doc);
        
        // Verify it's indented (contains newlines and spaces)
        Assert.Contains("\n", outputText);
        Assert.Contains("  ", outputText);
    }
}
