using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using clypse.portal.setup.Services.Security;
using Moq;

namespace clypse.portal.setup.UnitTests.Services.Security;

public class SecurityTokenServiceTests
{
    [Fact]
    public async Task GivenValidCredentials_WhenGetAccountIdAsync_ThenReturnsAccountId()
    {
        // Arrange
        var expectedAccountId = "123456789012";
        var mockSecurityTokenService = new Mock<IAmazonSecurityTokenService>();
        
        mockSecurityTokenService
            .Setup(sts => sts.GetCallerIdentityAsync(
                It.IsAny<GetCallerIdentityRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetCallerIdentityResponse
            {
                Account = expectedAccountId,
                UserId = "AIDAI123456789EXAMPLE",
                Arn = "arn:aws:iam::123456789012:user/test-user"
            });

        var sut = new SecurityTokenService(mockSecurityTokenService.Object);

        // Act
        var accountId = await sut.GetAccountIdAsync();

        // Assert
        Assert.Equal(expectedAccountId, accountId);
        mockSecurityTokenService.Verify(sts => sts.GetCallerIdentityAsync(
            It.IsAny<GetCallerIdentityRequest>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GivenValidCredentials_WhenGetAccountIdAsync_WithCancellationToken_ThenReturnsAccountId()
    {
        // Arrange
        var expectedAccountId = "987654321098";
        var cancellationToken = new CancellationToken();
        var mockSecurityTokenService = new Mock<IAmazonSecurityTokenService>();
        
        mockSecurityTokenService
            .Setup(sts => sts.GetCallerIdentityAsync(
                It.IsAny<GetCallerIdentityRequest>(),
                cancellationToken))
            .ReturnsAsync(new GetCallerIdentityResponse
            {
                Account = expectedAccountId,
                UserId = "AIDAI987654321EXAMPLE",
                Arn = "arn:aws:iam::987654321098:user/another-user"
            });

        var sut = new SecurityTokenService(mockSecurityTokenService.Object);

        // Act
        var accountId = await sut.GetAccountIdAsync(cancellationToken);

        // Assert
        Assert.Equal(expectedAccountId, accountId);
        mockSecurityTokenService.Verify(sts => sts.GetCallerIdentityAsync(
            It.IsAny<GetCallerIdentityRequest>(),
            cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task GivenInvalidCredentials_WhenGetAccountIdAsync_ThenThrowsException()
    {
        // Arrange
        var mockSecurityTokenService = new Mock<IAmazonSecurityTokenService>();
        
        mockSecurityTokenService
            .Setup(sts => sts.GetCallerIdentityAsync(
                It.IsAny<GetCallerIdentityRequest>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonSecurityTokenServiceException("Invalid security token"));

        var sut = new SecurityTokenService(mockSecurityTokenService.Object);

        // Act & Assert
        await Assert.ThrowsAsync<AmazonSecurityTokenServiceException>(
            async () => await sut.GetAccountIdAsync());
        
        mockSecurityTokenService.Verify(sts => sts.GetCallerIdentityAsync(
            It.IsAny<GetCallerIdentityRequest>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GivenCancelledOperation_WhenGetAccountIdAsync_ThenThrowsOperationCanceledException()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();
        var mockSecurityTokenService = new Mock<IAmazonSecurityTokenService>();
        
        mockSecurityTokenService
            .Setup(sts => sts.GetCallerIdentityAsync(
                It.IsAny<GetCallerIdentityRequest>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var sut = new SecurityTokenService(mockSecurityTokenService.Object);

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await sut.GetAccountIdAsync(cancellationTokenSource.Token));
        
        mockSecurityTokenService.Verify(sts => sts.GetCallerIdentityAsync(
            It.IsAny<GetCallerIdentityRequest>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
