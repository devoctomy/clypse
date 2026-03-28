using clypse.core.Cloud.Aws.S3;
using clypse.core.Vault;
using clypse.portal.Application.Services;
using Moq;

namespace clypse.portal.Application.UnitTests.Services;

public class VaultManagerBootstrapperFactoryServiceTests
{
    private readonly VaultManagerBootstrapperFactoryService sut;

    public VaultManagerBootstrapperFactoryServiceTests()
    {
        this.sut = new VaultManagerBootstrapperFactoryService();
    }

    [Fact]
    public void GivenNoParameters_WhenConstructing_ThenCreatesInstance()
    {
        // Assert
        Assert.NotNull(this.sut);
    }

    [Fact]
    public void GivenValidParameters_WhenCreateForBlazor_ThenReturnsBootstrapperService()
    {
        // Arrange
        var mockJsInvoker = new Mock<IJavaScriptS3Invoker>();

        // Act
        var result = this.sut.CreateForBlazor(
            mockJsInvoker.Object,
            "access-key",
            "secret-key",
            "session-token",
            "us-east-1",
            "my-bucket",
            "identity-id");

        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<IVaultManagerBootstrapperService>(result);
    }

    [Fact]
    public void GivenDifferentRegion_WhenCreateForBlazor_ThenReturnsBootstrapperService()
    {
        // Arrange
        var mockJsInvoker = new Mock<IJavaScriptS3Invoker>();

        // Act
        var result = this.sut.CreateForBlazor(
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

        // Act
        var result1 = this.sut.CreateForBlazor(
            mockJsInvoker.Object,
            "access-key-1",
            "secret-key-1",
            "session-token-1",
            "us-east-1",
            "bucket-1",
            "identity-1");
        var result2 = this.sut.CreateForBlazor(
            mockJsInvoker.Object,
            "access-key-2",
            "secret-key-2",
            "session-token-2",
            "us-west-2",
            "bucket-2",
            "identity-2");

        // Assert
        Assert.NotSame(result1, result2);
    }
}
