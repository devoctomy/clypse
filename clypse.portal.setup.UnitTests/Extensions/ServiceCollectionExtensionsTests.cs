using Amazon.CognitoIdentity;
using Amazon.CognitoIdentityProvider;
using Amazon.IdentityManagement;
using Amazon.S3;
using clypse.portal.setup.Extensions;
using clypse.portal.setup.Services;
using clypse.portal.setup.Services.Build;
using clypse.portal.setup.Services.Cloudfront;
using clypse.portal.setup.Services.Cognito;
using clypse.portal.setup.Services.Environment;
using clypse.portal.setup.Services.Iam;
using clypse.portal.setup.Services.Inventory;
using clypse.portal.setup.Services.IO;
using clypse.portal.setup.Services.Json;
using clypse.portal.setup.Services.Orchestration;
using clypse.portal.setup.Services.Process;
using clypse.portal.setup.Services.S3;
using clypse.portal.setup.Services.Security;
using clypse.portal.setup.Services.Upload;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace clypse.portal.setup.UnitTests.Extensions;

public class ServiceCollectionExtensionsTests
{
    private static Mock<IEnvironmentService> CreateMockEnvironmentService()
    {
        var mock = new Mock<IEnvironmentService>();
        mock.Setup(x => x.IsWindows).Returns(false);
        mock.Setup(x => x.GetEnvironmentVariable(It.IsAny<string>(), It.IsAny<EnvironmentVariableTarget>())).Returns((string?)null);
        return mock;
    }

    [Fact]
    public void GivenServiceCollection_WhenAddClypseSetupServices_ThenAllServicesAreRegistered()
    {
        // Arrange
        var services = new ServiceCollection();
        System.Environment.SetEnvironmentVariable("CLYPSE_SETUP_UNITTEST__BaseUrl", "http://localhost");
        System.Environment.SetEnvironmentVariable("CLYPSE_SETUP_UNITTEST__Region", "us-east-1");
        System.Environment.SetEnvironmentVariable("CLYPSE_SETUP_UNITTEST__AccessId", "test-access-id");
        System.Environment.SetEnvironmentVariable("CLYPSE_SETUP_UNITTEST__SecretAccessKey", "test-secret-key");
        System.Environment.SetEnvironmentVariable("CLYPSE_SETUP_UNITTEST__ResourcePrefix", "test-prefix");
        System.Environment.SetEnvironmentVariable("CLYPSE_SETUP_UNITTEST__InitialUserEmail", "foo@bar.com");

        var mockEnvService = CreateMockEnvironmentService();

        try
        {
            // Act
            var result = services.AddClypseSetupServices(
                "CLYPSE_SETUP_UNITTEST",
                Microsoft.Extensions.Logging.LogLevel.Information,
                mockEnvService.Object);

            // Assert
            Assert.NotNull(result);
            Assert.Same(services, result);

            var serviceProvider = services.BuildServiceProvider();

            // Verify AWS services
            AssertServiceRegistered<IAmazonS3>(serviceProvider);
            AssertServiceRegistered<IAmazonCognitoIdentity>(serviceProvider);
            AssertServiceRegistered<IAmazonCognitoIdentityProvider>(serviceProvider);
            AssertServiceRegistered<IAmazonIdentityManagementService>(serviceProvider);

            // Verify Clypse services
            AssertServiceRegistered<IIoService>(serviceProvider);
            AssertServiceRegistered<IProcessRunnerService>(serviceProvider);
            AssertServiceRegistered<ISecurityTokenService>(serviceProvider);
            AssertServiceRegistered<IS3Service>(serviceProvider);
            AssertServiceRegistered<IDirectoryUploadService>(serviceProvider);
            AssertServiceRegistered<ICognitoService>(serviceProvider);
            AssertServiceRegistered<IIamService>(serviceProvider);
            AssertServiceRegistered<ICloudfrontService>(serviceProvider);
            AssertServiceRegistered<IPortalBuildService>(serviceProvider);
            AssertServiceRegistered<ISetupInteractiveMenuService>(serviceProvider);
            AssertServiceRegistered<IClypseAwsSetupOrchestration>(serviceProvider);
            AssertServiceRegistered<IPortalConfigService>(serviceProvider);
            AssertServiceRegistered<IInventoryService>(serviceProvider);
            AssertServiceRegistered<IJsonMergerService>(serviceProvider);
            AssertServiceRegistered<IProgram>(serviceProvider);

            // Verify SetupOptions
            var options = serviceProvider.GetService<SetupOptions>();
            Assert.NotNull(options);
            Assert.Equal("http://localhost", options.BaseUrl);
            Assert.Equal("us-east-1", options.Region);
            Assert.Equal("test-access-id", options.AccessId);
            Assert.Equal("test-secret-key", options.SecretAccessKey);
            Assert.Equal("test-prefix", options.ResourcePrefix);
            Assert.Equal("foo@bar.com", options.InitialUserEmail);

            // Verify logging
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            Assert.NotNull(loggerFactory);
        }
        finally
        {
            // Cleanup
            System.Environment.SetEnvironmentVariable("CLYPSE_SETUP_UNITTEST__Region", null);
            System.Environment.SetEnvironmentVariable("CLYPSE_SETUP_UNITTEST__AccessId", null);
            System.Environment.SetEnvironmentVariable("CLYPSE_SETUP_UNITTEST__SecretAccessKey", null);
            System.Environment.SetEnvironmentVariable("CLYPSE_SETUP_UNITTEST__ResourcePrefix", null);
        }
    }

    [Fact]
    public void GivenServiceCollection_WhenAddClypseSetupServicesWithDefaultLogLevel_ThenServicesAreRegisteredWithDebugLogging()
    {
        // Arrange
        var services = new ServiceCollection();
        System.Environment.SetEnvironmentVariable("CLYPSE_SETUP_UNITTEST__Region", "eu-west-1");
        System.Environment.SetEnvironmentVariable("CLYPSE_SETUP_UNITTEST__AccessId", "test-access-id");
        System.Environment.SetEnvironmentVariable("CLYPSE_SETUP_UNITTEST__SecretAccessKey", "test-secret-key");
        System.Environment.SetEnvironmentVariable("CLYPSE_SETUP_UNITTEST__ResourcePrefix", "test-prefix");

        var mockEnvService = CreateMockEnvironmentService();

        try
        {
            // Act
            var result = services.AddClypseSetupServices("CLYPSE_SETUP_UNITTEST", environmentService: mockEnvService.Object);

            // Assert
            Assert.NotNull(result);
            var serviceProvider = services.BuildServiceProvider();

            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            Assert.NotNull(loggerFactory);

            var logger = loggerFactory.CreateLogger("Test");
            Assert.NotNull(logger);
        }
        finally
        {
            // Cleanup
            System.Environment.SetEnvironmentVariable("CLYPSE_SETUP_UNITTEST__Region", null);
            System.Environment.SetEnvironmentVariable("CLYPSE_SETUP_UNITTEST__AccessId", null);
            System.Environment.SetEnvironmentVariable("CLYPSE_SETUP_UNITTEST__SecretAccessKey", null);
            System.Environment.SetEnvironmentVariable("CLYPSE_SETUP_UNITTEST__ResourcePrefix", null);
        }
    }

    [Fact]
    public void GivenServiceCollectionWithBaseUrl_WhenAddClypseSetupServices_ThenAwsServicesUseCustomEndpoint()
    {
        // Arrange
        var services = new ServiceCollection();
        System.Environment.SetEnvironmentVariable("CLYPSE_SETUP_UNITTEST__Region", "us-west-2");
        System.Environment.SetEnvironmentVariable("CLYPSE_SETUP_UNITTEST__AccessId", "test-access-id");
        System.Environment.SetEnvironmentVariable("CLYPSE_SETUP_UNITTEST__SecretAccessKey", "test-secret-key");
        System.Environment.SetEnvironmentVariable("CLYPSE_SETUP_UNITTEST__ResourcePrefix", "test-prefix");
        System.Environment.SetEnvironmentVariable("CLYPSE_SETUP_UNITTEST__BaseUrl", "http://localhost:4566");

        var mockEnvService = CreateMockEnvironmentService();

        try
        {
            // Act
            services.AddClypseSetupServices("CLYPSE_SETUP_UNITTEST", environmentService: mockEnvService.Object);
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var options = serviceProvider.GetService<SetupOptions>();
            Assert.NotNull(options);
            Assert.Equal("http://localhost:4566", options.BaseUrl);

            // Verify services can still be resolved
            var s3Client = serviceProvider.GetService<IAmazonS3>();
            Assert.NotNull(s3Client);

            var cognitoIdentityClient = serviceProvider.GetService<IAmazonCognitoIdentity>();
            Assert.NotNull(cognitoIdentityClient);
        }
        finally
        {
            // Cleanup
            System.Environment.SetEnvironmentVariable("CLYPSE_SETUP_UNITTEST__Region", null);
            System.Environment.SetEnvironmentVariable("CLYPSE_SETUP_UNITTEST__AccessId", null);
            System.Environment.SetEnvironmentVariable("CLYPSE_SETUP_UNITTEST__SecretAccessKey", null);
            System.Environment.SetEnvironmentVariable("CLYPSE_SETUP_UNITTEST__ResourcePrefix", null);
            System.Environment.SetEnvironmentVariable("CLYPSE_SETUP_UNITTEST__BaseUrl", null);
        }
    }

    [Fact]
    public void GivenServiceCollection_WhenAddClypseSetupServicesWithNewEnvVars_ThenNewPropertiesAreSet()
    {
        // Arrange
        var services = new ServiceCollection();
        System.Environment.SetEnvironmentVariable("CLYPSE_SETUP_UNITTEST__Region", "us-east-1");
        System.Environment.SetEnvironmentVariable("CLYPSE_SETUP_UNITTEST__AccessId", "test-access-id");
        System.Environment.SetEnvironmentVariable("CLYPSE_SETUP_UNITTEST__SecretAccessKey", "test-secret-key");
        System.Environment.SetEnvironmentVariable("CLYPSE_SETUP_UNITTEST__ResourcePrefix", "test-prefix");
        System.Environment.SetEnvironmentVariable("CLYPSE_SETUP_UNITTEST__InitialUserEmail", "test@example.com");
        System.Environment.SetEnvironmentVariable("CLYPSE_SETUP_UNITTEST__EnableUpgradeMode", "true");
        System.Environment.SetEnvironmentVariable("CLYPSE_SETUP_UNITTEST__BuildPortal", "true");
        System.Environment.SetEnvironmentVariable("CLYPSE_SETUP_UNITTEST__ForceUpgrade", "true");
        System.Environment.SetEnvironmentVariable("CLYPSE_SETUP_UNITTEST__CloudFrontDistributionId", "E1234567890ABC");

        var mockEnvService = CreateMockEnvironmentService();

        try
        {
            // Act
            services.AddClypseSetupServices("CLYPSE_SETUP_UNITTEST", environmentService: mockEnvService.Object);
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var options = serviceProvider.GetService<SetupOptions>();
            Assert.NotNull(options);
            Assert.True(options.EnableUpgradeMode);
            Assert.True(options.BuildPortal);
            Assert.True(options.ForceUpgrade);
            Assert.Equal("E1234567890ABC", options.CloudFrontDistributionId);
        }
        finally
        {
            // Cleanup
            System.Environment.SetEnvironmentVariable("CLYPSE_SETUP_UNITTEST__Region", null);
            System.Environment.SetEnvironmentVariable("CLYPSE_SETUP_UNITTEST__AccessId", null);
            System.Environment.SetEnvironmentVariable("CLYPSE_SETUP_UNITTEST__SecretAccessKey", null);
            System.Environment.SetEnvironmentVariable("CLYPSE_SETUP_UNITTEST__ResourcePrefix", null);
            System.Environment.SetEnvironmentVariable("CLYPSE_SETUP_UNITTEST__InitialUserEmail", null);
            System.Environment.SetEnvironmentVariable("CLYPSE_SETUP_UNITTEST__EnableUpgradeMode", null);
            System.Environment.SetEnvironmentVariable("CLYPSE_SETUP_UNITTEST__BuildPortal", null);
            System.Environment.SetEnvironmentVariable("CLYPSE_SETUP_UNITTEST__ForceUpgrade", null);
            System.Environment.SetEnvironmentVariable("CLYPSE_SETUP_UNITTEST__CloudFrontDistributionId", null);
        }
    }

    [Fact]
    public void GivenServiceCollection_WhenAddClypseSetupServicesWithBuildPortalFalse_ThenBuildPortalIsFalse()
    {
        // Arrange
        var services = new ServiceCollection();
        System.Environment.SetEnvironmentVariable("CLYPSE_SETUP_UNITTEST__Region", "us-east-1");
        System.Environment.SetEnvironmentVariable("CLYPSE_SETUP_UNITTEST__AccessId", "test-access-id");
        System.Environment.SetEnvironmentVariable("CLYPSE_SETUP_UNITTEST__SecretAccessKey", "test-secret-key");
        System.Environment.SetEnvironmentVariable("CLYPSE_SETUP_UNITTEST__ResourcePrefix", "test-prefix");
        System.Environment.SetEnvironmentVariable("CLYPSE_SETUP_UNITTEST__InitialUserEmail", "test@example.com");
        System.Environment.SetEnvironmentVariable("CLYPSE_SETUP_UNITTEST__BuildPortal", "false");

        var mockEnvService = CreateMockEnvironmentService();

        try
        {
            // Act
            services.AddClypseSetupServices("CLYPSE_SETUP_UNITTEST", environmentService: mockEnvService.Object);
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var options = serviceProvider.GetService<SetupOptions>();
            Assert.NotNull(options);
            Assert.False(options.BuildPortal);
        }
        finally
        {
            // Cleanup
            System.Environment.SetEnvironmentVariable("CLYPSE_SETUP_UNITTEST__Region", null);
            System.Environment.SetEnvironmentVariable("CLYPSE_SETUP_UNITTEST__AccessId", null);
            System.Environment.SetEnvironmentVariable("CLYPSE_SETUP_UNITTEST__SecretAccessKey", null);
            System.Environment.SetEnvironmentVariable("CLYPSE_SETUP_UNITTEST__ResourcePrefix", null);
            System.Environment.SetEnvironmentVariable("CLYPSE_SETUP_UNITTEST__InitialUserEmail", null);
            System.Environment.SetEnvironmentVariable("CLYPSE_SETUP_UNITTEST__BuildPortal", null);
        }
    }

    private static void AssertServiceRegistered<TService>(IServiceProvider serviceProvider)
    {
        var service = serviceProvider.GetService<TService>();
        Assert.NotNull(service);
    }
}
