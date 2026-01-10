using Amazon.CognitoIdentity;
using Amazon.CognitoIdentityProvider;
using Amazon.IdentityManagement;
using Amazon.S3;
using clypse.portal.setup.Extensions;
using clypse.portal.setup.Services;
using clypse.portal.setup.Services.Cognito;
using clypse.portal.setup.Services.Iam;
using clypse.portal.setup.Services.Orchestration;
using clypse.portal.setup.Services.S3;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace clypse.portal.setup.UnitTests.Extensions;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void GivenServiceCollection_WhenAddClypseSetupServices_ThenAllServicesAreRegistered()
    {
        // Arrange
        var services = new ServiceCollection();
        Environment.SetEnvironmentVariable("CLYPSE_SETUP__BaseUrl", "http://localhost");
        Environment.SetEnvironmentVariable("CLYPSE_SETUP__Region", "us-east-1");
        Environment.SetEnvironmentVariable("CLYPSE_SETUP__AccessId", "test-access-id");
        Environment.SetEnvironmentVariable("CLYPSE_SETUP__SecretAccessKey", "test-secret-key");
        Environment.SetEnvironmentVariable("CLYPSE_SETUP__ResourcePrefix", "test-prefix");
        Environment.SetEnvironmentVariable("CLYPSE_SETUP__InitialUserEmail", "foo@bar.com");

        try
        {
            // Act
            var result = services.AddClypseSetupServices(Microsoft.Extensions.Logging.LogLevel.Information);

            // Assert
            Assert.NotNull(result);
            Assert.Same(services, result);

            var serviceProvider = services.BuildServiceProvider();

            // Verify AWS services
            var s3Client = serviceProvider.GetService<IAmazonS3>();
            Assert.NotNull(s3Client);

            var cognitoIdentityClient = serviceProvider.GetService<IAmazonCognitoIdentity>();
            Assert.NotNull(cognitoIdentityClient);

            var cognitoProviderClient = serviceProvider.GetService<IAmazonCognitoIdentityProvider>();
            Assert.NotNull(cognitoProviderClient);

            var iamClient = serviceProvider.GetService<IAmazonIdentityManagementService>();
            Assert.NotNull(iamClient);

            // Verify Clypse services
            var s3Service = serviceProvider.GetService<IS3Service>();
            Assert.NotNull(s3Service);

            var cognitoService = serviceProvider.GetService<ICognitoService>();
            Assert.NotNull(cognitoService);

            var iamService = serviceProvider.GetService<IIamService>();
            Assert.NotNull(iamService);

            var orchestration = serviceProvider.GetService<IClypseAwsSetupOrchestration>();
            Assert.NotNull(orchestration);

            var program = serviceProvider.GetService<IProgram>();
            Assert.NotNull(program);

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
            Environment.SetEnvironmentVariable("CLYPSE_SETUP__Region", null);
            Environment.SetEnvironmentVariable("CLYPSE_SETUP__AccessId", null);
            Environment.SetEnvironmentVariable("CLYPSE_SETUP__SecretAccessKey", null);
            Environment.SetEnvironmentVariable("CLYPSE_SETUP__ResourcePrefix", null);
        }
    }

    [Fact]
    public void GivenServiceCollection_WhenAddClypseSetupServicesWithDefaultLogLevel_ThenServicesAreRegisteredWithDebugLogging()
    {
        // Arrange
        var services = new ServiceCollection();
        Environment.SetEnvironmentVariable("CLYPSE_SETUP__Region", "eu-west-1");
        Environment.SetEnvironmentVariable("CLYPSE_SETUP__AccessId", "test-access-id");
        Environment.SetEnvironmentVariable("CLYPSE_SETUP__SecretAccessKey", "test-secret-key");
        Environment.SetEnvironmentVariable("CLYPSE_SETUP__ResourcePrefix", "test-prefix");

        try
        {
            // Act
            var result = services.AddClypseSetupServices();

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
            Environment.SetEnvironmentVariable("CLYPSE_SETUP__Region", null);
            Environment.SetEnvironmentVariable("CLYPSE_SETUP__AccessId", null);
            Environment.SetEnvironmentVariable("CLYPSE_SETUP__SecretAccessKey", null);
            Environment.SetEnvironmentVariable("CLYPSE_SETUP__ResourcePrefix", null);
        }
    }

    [Fact]
    public void GivenServiceCollectionWithBaseUrl_WhenAddClypseSetupServices_ThenAwsServicesUseCustomEndpoint()
    {
        // Arrange
        var services = new ServiceCollection();
        Environment.SetEnvironmentVariable("CLYPSE_SETUP__Region", "us-west-2");
        Environment.SetEnvironmentVariable("CLYPSE_SETUP__AccessId", "test-access-id");
        Environment.SetEnvironmentVariable("CLYPSE_SETUP__SecretAccessKey", "test-secret-key");
        Environment.SetEnvironmentVariable("CLYPSE_SETUP__ResourcePrefix", "test-prefix");
        Environment.SetEnvironmentVariable("CLYPSE_SETUP__BaseUrl", "http://localhost:4566");

        try
        {
            // Act
            services.AddClypseSetupServices();
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
            Environment.SetEnvironmentVariable("CLYPSE_SETUP__Region", null);
            Environment.SetEnvironmentVariable("CLYPSE_SETUP__AccessId", null);
            Environment.SetEnvironmentVariable("CLYPSE_SETUP__SecretAccessKey", null);
            Environment.SetEnvironmentVariable("CLYPSE_SETUP__ResourcePrefix", null);
            Environment.SetEnvironmentVariable("CLYPSE_SETUP__BaseUrl", null);
        }
    }
}
