using clypse.portal.setup.Services.Build;
using clypse.portal.setup.Services.Cloudfront;
using clypse.portal.setup.Services.Cognito;
using clypse.portal.setup.Services.Iam;
using clypse.portal.setup.Services.Orchestration;
using clypse.portal.setup.Services.S3;
using clypse.portal.setup.Services.Security;
using Microsoft.Extensions.Logging;
using Moq;

namespace clypse.portal.setup.UnitTests.Services.Orchestration;

public class ClypseAwsSetupOrchestrationTests
{
    private readonly Mock<ISecurityTokenService> _mockSecurityTokenService;
    private readonly Mock<IIamService> _mockIamService;
    private readonly Mock<IS3Service> _mockS3Service;
    private readonly Mock<ICognitoService> _mockCognitoService;
    private readonly Mock<ICloudfrontService> _mockCloudfrontService;
    private readonly Mock<IPortalConfigService> _mockPortalConfigService;
    private readonly Mock<ILogger<IamService>> _mockLogger;
    private readonly SetupOptions _options;

    public ClypseAwsSetupOrchestrationTests()
    {
        _mockSecurityTokenService = new Mock<ISecurityTokenService>();
        _mockIamService = new Mock<IIamService>();
        _mockS3Service = new Mock<IS3Service>();
        _mockCognitoService = new Mock<ICognitoService>();
        _mockCloudfrontService = new Mock<ICloudfrontService>();
        _mockPortalConfigService = new Mock<IPortalConfigService>();
        _mockLogger = new Mock<ILogger<IamService>>();
        _options = new SetupOptions
        {
            BaseUrl = "http://localhost:4566",
            Region = "us-east-1",
            ResourcePrefix = "test-prefix",
            AccessId = "test-access-id",
            SecretAccessKey = "test-secret-key",
            PortalBuildOutputPath = "test-output-path",
            InitialUserEmail = "test@example.com",
            InteractiveMode = false
        };
    }

    private ClypseAwsSetupOrchestration CreateSut()
    {
        return new ClypseAwsSetupOrchestration(
            _options,
            _mockSecurityTokenService.Object,
            _mockIamService.Object,
            _mockS3Service.Object,
            _mockCognitoService.Object,
            _mockCloudfrontService.Object,
            _mockPortalConfigService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task GivenValidOptions_AndBucketsDoNotExist_WhenPrepareSetup_ThenReturnsTrue()
    {
        // Arrange
        var sut = CreateSut();
        _mockS3Service
            .Setup(s => s.DoesBucketExistAsync("clypse.portal", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockS3Service
            .Setup(s => s.DoesBucketExistAsync("clypse.data", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await sut.PrepareSetup(CancellationToken.None);

        // Assert
        Assert.True(result);
        _mockS3Service.Verify(s => s.DoesBucketExistAsync("clypse.portal", It.IsAny<CancellationToken>()), Times.Once);
        _mockS3Service.Verify(s => s.DoesBucketExistAsync("clypse.data", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenValidOptions_AndPortalBucketExists_WhenPrepareSetup_ThenReturnsFalse()
    {
        // Arrange
        var sut = CreateSut();
        _mockS3Service
            .Setup(s => s.DoesBucketExistAsync("clypse.portal", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockS3Service
            .Setup(s => s.DoesBucketExistAsync("clypse.data", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await sut.PrepareSetup(CancellationToken.None);

        // Assert
        Assert.False(result);
        _mockS3Service.Verify(s => s.DoesBucketExistAsync("clypse.portal", It.IsAny<CancellationToken>()), Times.Once);
        _mockS3Service.Verify(s => s.DoesBucketExistAsync("clypse.data", It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GivenValidOptions_AndDataBucketExists_WhenPrepareSetup_ThenReturnsFalse()
    {
        // Arrange
        var sut = CreateSut();
        _mockS3Service
            .Setup(s => s.DoesBucketExistAsync("clypse.portal", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockS3Service
            .Setup(s => s.DoesBucketExistAsync("clypse.data", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await sut.PrepareSetup(CancellationToken.None);

        // Assert
        Assert.False(result);
        _mockS3Service.Verify(s => s.DoesBucketExistAsync("clypse.portal", It.IsAny<CancellationToken>()), Times.Once);
        _mockS3Service.Verify(s => s.DoesBucketExistAsync("clypse.data", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenInvalidOptions_WhenPrepareSetup_ThenThrowsException()
    {
        // Arrange
        var invalidOptions = new SetupOptions(); // Invalid options
        var sut = new ClypseAwsSetupOrchestration(
            invalidOptions,
            _mockSecurityTokenService.Object,
            _mockIamService.Object,
            _mockS3Service.Object,
            _mockCognitoService.Object,
            _mockCloudfrontService.Object,
            _mockPortalConfigService.Object,
            _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => sut.PrepareSetup(CancellationToken.None));
    }

    [Fact]
    public async Task GivenInvalidOptions_WhenSetupClypseOnAwsAsync_ThenThrowsException()
    {
        // Arrange
        var invalidOptions = new SetupOptions(); // Invalid options
        var sut = new ClypseAwsSetupOrchestration(
            invalidOptions,
            _mockSecurityTokenService.Object,
            _mockIamService.Object,
            _mockS3Service.Object,
            _mockCognitoService.Object,
            _mockCloudfrontService.Object,
            _mockPortalConfigService.Object,
            _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => sut.SetupClypseOnAwsAsync(CancellationToken.None));
    }

    [Fact]
    public async Task GivenValidOptions_AndPortalBucketCreationFails_WhenSetupClypseOnAwsAsync_ThenThrowsException()
    {
        // Arrange
        var sut = CreateSut();
        _mockSecurityTokenService
            .Setup(s => s.GetAccountIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("123456789012");
        _mockS3Service
            .Setup(s => s.CreateBucketAsync("clypse.portal", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => sut.SetupClypseOnAwsAsync(CancellationToken.None));
        Assert.Equal("Failed to create S3 bucket for portal.", exception.Message);
    }

    [Fact]
    public async Task GivenValidOptions_AndPortalBucketTaggingFails_WhenSetupClypseOnAwsAsync_ThenThrowsException()
    {
        // Arrange
        var sut = CreateSut();
        _mockSecurityTokenService
            .Setup(s => s.GetAccountIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("123456789012");
        _mockS3Service
            .Setup(s => s.CreateBucketAsync("clypse.portal", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockS3Service
            .Setup(s => s.SetBucketTags("clypse.portal", It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => sut.SetupClypseOnAwsAsync(CancellationToken.None));
        Assert.Equal("Failed to set tags for portal bucket.", exception.Message);
    }

    [Fact]
    public async Task GivenValidOptions_AndPortalBucketPolicyFails_WhenSetupClypseOnAwsAsync_ThenThrowsException()
    {
        // Arrange
        var sut = CreateSut();
        _mockSecurityTokenService
            .Setup(s => s.GetAccountIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("123456789012");
        _mockS3Service
            .Setup(s => s.CreateBucketAsync("clypse.portal", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockS3Service
            .Setup(s => s.SetBucketTags("clypse.portal", It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockS3Service
            .Setup(s => s.SetBucketPolicyAsync("clypse.portal", It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => sut.SetupClypseOnAwsAsync(CancellationToken.None));
        Assert.Equal("Failed to set portal bucket policy.", exception.Message);
    }

    [Fact]
    public async Task GivenValidOptions_AndDataBucketCreationFails_WhenSetupClypseOnAwsAsync_ThenThrowsException()
    {
        // Arrange
        var sut = CreateSut();
        _mockSecurityTokenService
            .Setup(s => s.GetAccountIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("123456789012");
        _mockS3Service
            .Setup(s => s.CreateBucketAsync("clypse.portal", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockS3Service
            .Setup(s => s.SetBucketTags("clypse.portal", It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockS3Service
            .Setup(s => s.SetBucketPolicyAsync("clypse.portal", It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockS3Service
            .Setup(s => s.CreateBucketAsync("clypse.data", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => sut.SetupClypseOnAwsAsync(CancellationToken.None));
        Assert.Equal("Failed to create S3 bucket for data.", exception.Message);
    }

    [Fact]
    public async Task GivenValidOptions_AndDataBucketTaggingFails_WhenSetupClypseOnAwsAsync_ThenThrowsException()
    {
        // Arrange
        var sut = CreateSut();
        _mockSecurityTokenService
            .Setup(s => s.GetAccountIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("123456789012");
        _mockS3Service
            .Setup(s => s.CreateBucketAsync("clypse.portal", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockS3Service
            .Setup(s => s.SetBucketTags("clypse.portal", It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockS3Service
            .Setup(s => s.SetBucketPolicyAsync("clypse.portal", It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockS3Service
            .Setup(s => s.CreateBucketAsync("clypse.data", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockS3Service
            .Setup(s => s.SetBucketTags("clypse.data", It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => sut.SetupClypseOnAwsAsync(CancellationToken.None));
        Assert.Equal("Failed to set tags for data bucket.", exception.Message);
    }

    [Fact]
    public async Task GivenValidOptions_AndUserPoolClientCreationFails_WhenSetupClypseOnAwsAsync_ThenThrowsException()
    {
        // Arrange
        var sut = CreateSut();
        SetupSuccessfulS3Operations();
        SetupSuccessfulIamOperations();
        SetupSuccessfulCognitoUserPoolCreation();
        
        _mockCognitoService
            .Setup(s => s.CreateUserPoolClientAsync(
                It.IsAny<string>(),
                "clypse.identity.pool.client",
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(string.Empty);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => sut.SetupClypseOnAwsAsync(CancellationToken.None));
        Assert.Equal("Failed to create user pool client.", exception.Message);
    }

    [Fact]
    public async Task GivenValidOptions_AndAttachDataPolicyFails_WhenSetupClypseOnAwsAsync_ThenThrowsException()
    {
        // Arrange
        var sut = CreateSut();
        SetupSuccessfulS3Operations();
        SetupSuccessfulIamPolicyCreation();
        SetupSuccessfulCognitoOperations();
        
        _mockIamService
            .Setup(s => s.CreateRoleAsync(
                "clypse.auth.role",
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("arn:aws:iam::123456789012:role/test-prefix.clypse.auth.role");
        _mockIamService
            .Setup(s => s.AttachPolicyToRoleAsync(
                "clypse.auth.role",
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => sut.SetupClypseOnAwsAsync(CancellationToken.None));
        Assert.Equal("Failed to attach data policy to auth role.", exception.Message);
    }

    [Fact]
    public async Task GivenValidOptions_AndAttachAuthPolicyFails_WhenSetupClypseOnAwsAsync_ThenThrowsException()
    {
        // Arrange
        var sut = CreateSut();
        SetupSuccessfulS3Operations();
        SetupSuccessfulIamPolicyCreation();
        SetupSuccessfulCognitoOperations();
        
        _mockIamService
            .Setup(s => s.CreateRoleAsync(
                "clypse.auth.role",
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("arn:aws:iam::123456789012:role/test-prefix.clypse.auth.role");
        
        var firstCall = true;
        _mockIamService
            .Setup(s => s.AttachPolicyToRoleAsync(
                "clypse.auth.role",
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => {
                if (firstCall)
                {
                    firstCall = false;
                    return true; // First call succeeds (data policy)
                }
                return false; // Second call fails (auth policy)
            });

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => sut.SetupClypseOnAwsAsync(CancellationToken.None));
        Assert.Equal("Failed to attach auth policy to auth role.", exception.Message);
    }

    [Fact]
    public async Task GivenValidOptions_AndSetAuthenticatedRoleFails_WhenSetupClypseOnAwsAsync_ThenThrowsException()
    {
        // Arrange
        var sut = CreateSut();
        SetupSuccessfulS3Operations();
        SetupSuccessfulIamOperations();
        SetupSuccessfulCognitoOperations();
        
        _mockCognitoService
            .Setup(s => s.SetIdentityPoolAuthenticatedRoleAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => sut.SetupClypseOnAwsAsync(CancellationToken.None));
        Assert.Equal("Failed to set authenticated role for identity pool.", exception.Message);
    }

    [Fact]
    public async Task GivenValidOptions_AndSetBucketWebsiteConfigFails_WhenSetupClypseOnAwsAsync_ThenThrowsException()
    {
        // Arrange
        var sut = CreateSut();
        SetupSuccessfulS3Operations();
        SetupSuccessfulIamOperations();
        SetupSuccessfulCognitoOperations();
        
        _mockS3Service
            .Setup(s => s.SetBucketWebsiteConfigurationAsync(
                "clypse.portal",
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => sut.SetupClypseOnAwsAsync(CancellationToken.None));
        Assert.Equal("Failed to set portal bucket website configuration.", exception.Message);
    }

    [Fact]
    public async Task GivenValidOptions_AndCloudfrontCreationFails_WhenSetupClypseOnAwsAsync_ThenThrowsException()
    {
        // Arrange
        var sut = CreateSut();
        SetupSuccessfulS3Operations();
        SetupSuccessfulIamOperations();
        SetupSuccessfulCognitoOperations();
        
        _mockS3Service
            .Setup(s => s.SetBucketWebsiteConfigurationAsync(
                "clypse.portal",
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockCloudfrontService
            .Setup(s => s.CreateDistributionAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(string.Empty);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => sut.SetupClypseOnAwsAsync(CancellationToken.None));
        Assert.Equal("Failed to create CloudFront distribution.", exception.Message);
    }

    [Fact]
    public async Task GivenValidOptions_AndSetDataBucketCorsFails_WhenSetupClypseOnAwsAsync_ThenThrowsException()
    {
        // Arrange
        var sut = CreateSut();
        SetupSuccessfulS3Operations();
        SetupSuccessfulIamOperations();
        SetupSuccessfulCognitoOperations();
        SetupSuccessfulCloudfrontOperations();
        
        _mockS3Service
            .Setup(s => s.SetBucketCorsConfigurationAsync(
                "clypse.data",
                It.IsAny<List<string>>(),
                It.IsAny<List<string>>(),
                It.IsAny<List<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => sut.SetupClypseOnAwsAsync(CancellationToken.None));
        Assert.Contains("CORS", exception.Message);
    }

    [Fact]
    public async Task GivenValidOptions_AndCreateInitialUserFails_WhenSetupClypseOnAwsAsync_ThenThrowsException()
    {
        // Arrange
        var sut = CreateSut();
        SetupSuccessfulS3Operations();
        SetupSuccessfulIamOperations();
        SetupSuccessfulCognitoOperations();
        SetupSuccessfulCloudfrontOperations();
        SetupSuccessfulCorsConfiguration();
        
        _mockCognitoService
            .Setup(s => s.CreateUserAsync(
                _options.InitialUserEmail,
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => sut.SetupClypseOnAwsAsync(CancellationToken.None));
        Assert.Equal("Failed to create initial user.", exception.Message);
    }

    [Fact]
    public async Task GivenValidOptions_AndAllOperationsSucceed_WhenSetupClypseOnAwsAsync_ThenReturnsTrue()
    {
        // Arrange
        var sut = CreateSut();
        SetupSuccessfulS3Operations();
        SetupSuccessfulIamOperations();
        SetupSuccessfulCognitoOperations();
        SetupSuccessfulCloudfrontOperations();
        SetupSuccessfulCorsConfiguration();
        
        _mockCognitoService
            .Setup(s => s.CreateUserAsync(
                _options.InitialUserEmail,
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await sut.SetupClypseOnAwsAsync(CancellationToken.None);

        // Assert
        Assert.True(result);
        VerifyAllServicesWereCalled();
    }

    private void SetupSuccessfulS3Operations()
    {
        _mockSecurityTokenService
            .Setup(s => s.GetAccountIdAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("123456789012");

        _mockS3Service
            .Setup(s => s.CreateBucketAsync("clypse.portal", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockS3Service
            .Setup(s => s.SetBucketTags("clypse.portal", It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockS3Service
            .Setup(s => s.SetBucketPolicyAsync("clypse.portal", It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockS3Service
            .Setup(s => s.CreateBucketAsync("clypse.data", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockS3Service
            .Setup(s => s.SetBucketTags("clypse.data", It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockS3Service
            .Setup(s => s.SetBucketWebsiteConfigurationAsync("clypse.portal", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockS3Service
            .Setup(s => s.UploadDirectoryToBucket(
                "clypse.portal",
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
    }

    private void SetupSuccessfulIamPolicyCreation()
    {
        _mockIamService
            .Setup(s => s.CreatePolicyAsync(
                "clypse.data.policy",
                It.IsAny<object>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("arn:aws:iam::123456789012:policy/test-prefix.clypse.data.policy");
        _mockIamService
            .Setup(s => s.CreatePolicyAsync(
                "clypse.auth.policy",
                It.IsAny<object>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("arn:aws:iam::123456789012:policy/test-prefix.clypse.auth.policy");
    }

    private void SetupSuccessfulIamOperations()
    {
        SetupSuccessfulIamPolicyCreation();
        
        _mockIamService
            .Setup(s => s.CreateRoleAsync(
                "clypse.auth.role",
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("arn:aws:iam::123456789012:role/test-prefix.clypse.auth.role");
        _mockIamService
            .Setup(s => s.AttachPolicyToRoleAsync(
                "clypse.auth.role",
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
    }

    private void SetupSuccessfulCognitoUserPoolCreation()
    {
        _mockCognitoService
            .Setup(s => s.CreateUserPoolAsync(
                "clypse.user.pool",
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("us-east-1_ABC123DEF");
    }

    private void SetupSuccessfulCognitoOperations()
    {
        SetupSuccessfulCognitoUserPoolCreation();
        
        _mockCognitoService
            .Setup(s => s.CreateUserPoolClientAsync(
                It.IsAny<string>(),
                "clypse.identity.pool.client",
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("1234567890abcdef1234567890");
        _mockCognitoService
            .Setup(s => s.CreateIdentityPoolAsync(
                "clypse.identity.pool",
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("us-east-1:12345678-1234-1234-1234-123456789012");
        _mockCognitoService
            .Setup(s => s.SetIdentityPoolAuthenticatedRoleAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
    }

    private void SetupSuccessfulCloudfrontOperations()
    {
        _mockCloudfrontService
            .Setup(s => s.CreateDistributionAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("d1234567890abc.cloudfront.net");
    }

    private void SetupSuccessfulCorsConfiguration()
    {
        _mockS3Service
            .Setup(s => s.SetBucketCorsConfigurationAsync(
                "clypse.portal",
                It.IsAny<List<string>>(),
                It.IsAny<List<string>>(),
                It.IsAny<List<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockS3Service
            .Setup(s => s.SetBucketCorsConfigurationAsync(
                "clypse.data",
                It.IsAny<List<string>>(),
                It.IsAny<List<string>>(),
                It.IsAny<List<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
    }

    private void VerifyAllServicesWereCalled()
    {
        // Verify SecurityTokenService
        _mockSecurityTokenService.Verify(s => s.GetAccountIdAsync(It.IsAny<CancellationToken>()), Times.Once);

        // Verify S3Service
        _mockS3Service.Verify(s => s.CreateBucketAsync("clypse.portal", true, It.IsAny<CancellationToken>()), Times.Once);
        _mockS3Service.Verify(s => s.CreateBucketAsync("clypse.data", false, It.IsAny<CancellationToken>()), Times.Once);
        _mockS3Service.Verify(s => s.SetBucketTags("clypse.portal", It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockS3Service.Verify(s => s.SetBucketTags("clypse.data", It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockS3Service.Verify(s => s.SetBucketWebsiteConfigurationAsync("clypse.portal", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);

        // Verify IamService
        _mockIamService.Verify(s => s.CreatePolicyAsync("clypse.data.policy", It.IsAny<object>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockIamService.Verify(s => s.CreatePolicyAsync("clypse.auth.policy", It.IsAny<object>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockIamService.Verify(s => s.CreateRoleAsync("clypse.auth.role", It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockIamService.Verify(s => s.AttachPolicyToRoleAsync("clypse.auth.role", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));

        // Verify CognitoService
        _mockCognitoService.Verify(s => s.CreateUserPoolAsync("clypse.user.pool", It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockCognitoService.Verify(s => s.CreateUserPoolClientAsync(It.IsAny<string>(), "clypse.identity.pool.client", It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockCognitoService.Verify(s => s.CreateIdentityPoolAsync("clypse.identity.pool", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockCognitoService.Verify(s => s.SetIdentityPoolAuthenticatedRoleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockCognitoService.Verify(s => s.CreateUserAsync(_options.InitialUserEmail, It.IsAny<string>(), It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()), Times.Once);

        // Verify CloudfrontService
        _mockCloudfrontService.Verify(s => s.CreateDistributionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
