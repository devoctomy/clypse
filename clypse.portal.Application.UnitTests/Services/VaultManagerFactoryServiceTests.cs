using clypse.core.Cloud.Aws.S3;
using clypse.core.Cryptography;
using clypse.core.Vault;
using clypse.portal.Application.Services;
using Moq;

namespace clypse.portal.Application.UnitTests.Services;

public class VaultManagerFactoryServiceTests
{
    private readonly KeyDerivationServiceOptions keyDerivationOptions;

    public VaultManagerFactoryServiceTests()
    {
        this.keyDerivationOptions = new KeyDerivationServiceOptions();
        this.keyDerivationOptions.Parameters["algorithm"] = "Argon2id";
        this.keyDerivationOptions.Parameters["iterations"] = 1;
        this.keyDerivationOptions.Parameters["memory"] = 65536;
        this.keyDerivationOptions.Parameters["parallelism"] = 1;
        this.keyDerivationOptions.Parameters["saltLength"] = 16;
        this.keyDerivationOptions.Parameters["keyLength"] = 32;
    }

    private VaultManagerFactoryService CreateSut()
    {
        return new VaultManagerFactoryService(this.keyDerivationOptions);
    }

    [Fact]
    public void GivenValidOptions_WhenConstructing_ThenCreatesInstance()
    {
        // Act
        var sut = this.CreateSut();

        // Assert
        Assert.NotNull(sut);
    }

    [Fact]
    public void GivenValidParameters_WhenCreateForBlazor_ThenReturnsVaultManager()
    {
        // Arrange
        var mockJsInvoker = new Mock<IJavaScriptS3Invoker>();
        var sut = this.CreateSut();

        // Act
        var result = sut.CreateForBlazor(
            mockJsInvoker.Object,
            "access-key",
            "secret-key",
            "session-token",
            "us-east-1",
            "my-bucket",
            "identity-id");

        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<IVaultManager>(result);
    }

    [Fact]
    public void GivenDifferentRegion_WhenCreateForBlazor_ThenReturnsVaultManager()
    {
        // Arrange
        var mockJsInvoker = new Mock<IJavaScriptS3Invoker>();
        var sut = this.CreateSut();

        // Act
        var result = sut.CreateForBlazor(
            mockJsInvoker.Object,
            "access-key",
            "secret-key",
            "session-token",
            "eu-west-1",
            "eu-bucket",
            "eu-identity-id");

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void GivenMultipleCalls_WhenCreateForBlazor_ThenReturnsDistinctInstances()
    {
        // Arrange
        var mockJsInvoker = new Mock<IJavaScriptS3Invoker>();
        var sut = this.CreateSut();

        // Act
        var result1 = sut.CreateForBlazor(
            mockJsInvoker.Object,
            "key-1",
            "secret-1",
            "token-1",
            "us-east-1",
            "bucket-1",
            "identity-1");
        var result2 = sut.CreateForBlazor(
            mockJsInvoker.Object,
            "key-2",
            "secret-2",
            "token-2",
            "us-west-2",
            "bucket-2",
            "identity-2");

        // Assert
        Assert.NotSame(result1, result2);
    }
}
