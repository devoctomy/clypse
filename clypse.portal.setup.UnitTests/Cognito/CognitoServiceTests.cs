using Amazon.CognitoIdentity;
using Amazon.CognitoIdentity.Model;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using clypse.portal.setup.Cognito;
using Moq;

namespace clypse.portal.setup.UnitTests.Cognito;

public class CognitoServiceTests
{
    [Fact]
    public async Task GivenName_WhenCreateIdentityPool_ThenCreatesIdentityPool()
    {
        // Arrange
        var mockCognitoIdentity = new Mock<IAmazonCognitoIdentity>();
        var mockCognitoIdentityProvider = new Mock<IAmazonCognitoIdentityProvider>();
        var cognitoService = new CognitoService(
            mockCognitoIdentity.Object,
            mockCognitoIdentityProvider.Object);
        var identityPoolName = "test-identity-pool";
        var expectedIdentityPoolId = "us-east-1:12345678-1234-1234-1234-123456789012";

        mockCognitoIdentity
            .Setup(c => c.CreateIdentityPoolAsync(
                It.Is<CreateIdentityPoolRequest>(req => req.IdentityPoolName == identityPoolName),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateIdentityPoolResponse
            {
                IdentityPoolId = expectedIdentityPoolId
            });
        
        // Act
        var identityPoolId = await cognitoService.CreateIdentityPoolAsync(identityPoolName);

        // Assert
        Assert.Equal(expectedIdentityPoolId, identityPoolId);
        mockCognitoIdentity.Verify(c => c.CreateIdentityPoolAsync(
            It.Is<CreateIdentityPoolRequest>(req => req.IdentityPoolName == identityPoolName),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GivenName_WhenCreateUserPool_ThenCreatesUserPool()
    {
        // Arrange
        var mockCognitoIdentity = new Mock<IAmazonCognitoIdentity>();
        var mockCognitoIdentityProvider = new Mock<IAmazonCognitoIdentityProvider>();
        var cognitoService = new CognitoService(
            mockCognitoIdentity.Object,
            mockCognitoIdentityProvider.Object);
        var userPoolName = "test-user-pool";
        var expectedUserPoolId = "us-east-1_ABC123DEF";

        mockCognitoIdentityProvider
            .Setup(c => c.CreateUserPoolAsync(
                It.Is<CreateUserPoolRequest>(req => req.PoolName == userPoolName),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateUserPoolResponse
            {
                UserPool = new UserPoolType
                {
                    Id = expectedUserPoolId
                }
            });
        
        // Act
        var userPoolId = await cognitoService.CreateUserPoolAsync(userPoolName);

        // Assert
        Assert.Equal(expectedUserPoolId, userPoolId);
        mockCognitoIdentityProvider.Verify(c => c.CreateUserPoolAsync(
            It.Is<CreateUserPoolRequest>(req => req.PoolName == userPoolName),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GivenUserPoolIdAndName_WhenCreateUserPoolClient_ThenCreatesUserPoolClient()
    {
        // Arrange
        var mockCognitoIdentity = new Mock<IAmazonCognitoIdentity>();
        var mockCognitoIdentityProvider = new Mock<IAmazonCognitoIdentityProvider>();
        var cognitoService = new CognitoService(
            mockCognitoIdentity.Object,
            mockCognitoIdentityProvider.Object);
        var userPoolId = "us-east-1_ABC123DEF";
        var clientName = "test-client";
        var expectedClientId = "1234567890abcdef1234567890";

        mockCognitoIdentityProvider
            .Setup(c => c.CreateUserPoolClientAsync(
                It.Is<CreateUserPoolClientRequest>(req => 
                    req.UserPoolId == userPoolId && 
                    req.ClientName == clientName),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateUserPoolClientResponse
            {
                UserPoolClient = new UserPoolClientType
                {
                    ClientId = expectedClientId
                }
            });
        
        // Act
        var clientId = await cognitoService.CreateUserPoolClientAsync(userPoolId, clientName);

        // Assert
        Assert.Equal(expectedClientId, clientId);
        mockCognitoIdentityProvider.Verify(c => c.CreateUserPoolClientAsync(
            It.Is<CreateUserPoolClientRequest>(req => 
                req.UserPoolId == userPoolId && 
                req.ClientName == clientName),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
