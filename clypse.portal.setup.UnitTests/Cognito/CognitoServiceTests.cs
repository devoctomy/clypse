using Amazon.CognitoIdentity;
using Amazon.CognitoIdentity.Model;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using clypse.portal.setup.Cognito;
using Microsoft.Extensions.Logging;
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
        var options = new AwsServiceOptions
        {
            ResourcePrefix = "test-prefix"
        };
        var sut = new CognitoService(
            mockCognitoIdentity.Object,
            mockCognitoIdentityProvider.Object,
            options,
            Mock.Of<ILogger<CognitoService>>());
        var identityPoolName = "test-identity-pool";
        var expecvtedIdentityPoolName = "test-prefix.test-identity-pool";
        var expectedIdentityPoolId = "us-east-1:12345678-1234-1234-1234-123456789012";

        mockCognitoIdentity
            .Setup(c => c.CreateIdentityPoolAsync(
                It.Is<CreateIdentityPoolRequest>(req => req.IdentityPoolName == expecvtedIdentityPoolName),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateIdentityPoolResponse
            {
                IdentityPoolId = expectedIdentityPoolId
            });
        
        // Act
        var identityPoolId = await sut.CreateIdentityPoolAsync(identityPoolName);

        // Assert
        Assert.Equal(expectedIdentityPoolId, identityPoolId);
        mockCognitoIdentity.Verify(c => c.CreateIdentityPoolAsync(
            It.Is<CreateIdentityPoolRequest>(req => req.IdentityPoolName == expecvtedIdentityPoolName),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GivenName_WhenCreateUserPool_ThenCreatesUserPool()
    {
        // Arrange
        var mockCognitoIdentity = new Mock<IAmazonCognitoIdentity>();
        var mockCognitoIdentityProvider = new Mock<IAmazonCognitoIdentityProvider>();
        var options = new AwsServiceOptions
        {
            ResourcePrefix = "test-prefix"
        };
        var sut = new CognitoService(
            mockCognitoIdentity.Object,
            mockCognitoIdentityProvider.Object,
            options,
            Mock.Of<ILogger<CognitoService>>());
        var userPoolName = "test-user-pool";
        var expecteduserPoolName = "test-prefix.test-user-pool";
        var expectedUserPoolId = "us-east-1_ABC123DEF";

        mockCognitoIdentityProvider
            .Setup(c => c.CreateUserPoolAsync(
                It.Is<CreateUserPoolRequest>(req => req.PoolName == expecteduserPoolName),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateUserPoolResponse
            {
                UserPool = new UserPoolType
                {
                    Id = expectedUserPoolId
                }
            });
        
        // Act
        var userPoolId = await sut.CreateUserPoolAsync(userPoolName);

        // Assert
        Assert.Equal(expectedUserPoolId, userPoolId);
        mockCognitoIdentityProvider.Verify(c => c.CreateUserPoolAsync(
            It.Is<CreateUserPoolRequest>(req => req.PoolName == expecteduserPoolName),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GivenUserPoolIdAndName_WhenCreateUserPoolClient_ThenCreatesUserPoolClient()
    {
        // Arrange
        var mockCognitoIdentity = new Mock<IAmazonCognitoIdentity>();
        var mockCognitoIdentityProvider = new Mock<IAmazonCognitoIdentityProvider>();
        var options = new AwsServiceOptions
        {
            ResourcePrefix = "test-prefix"
        };
        var sut = new CognitoService(
            mockCognitoIdentity.Object,
            mockCognitoIdentityProvider.Object,
            options,
            Mock.Of<ILogger<CognitoService>>());
        var userPoolId = "us-east-1_ABC123DEF";
        var clientName = "test-client";
        var expectedClientName = "test-prefix.test-client";
        var expectedClientId = "1234567890abcdef1234567890";

        mockCognitoIdentityProvider
            .Setup(c => c.CreateUserPoolClientAsync(
                It.Is<CreateUserPoolClientRequest>(req => 
                    req.UserPoolId == userPoolId && 
                    req.ClientName == expectedClientName),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateUserPoolClientResponse
            {
                UserPoolClient = new UserPoolClientType
                {
                    ClientId = expectedClientId
                }
            });
        
        // Act
        var clientId = await sut.CreateUserPoolClientAsync(clientName, userPoolId);

        // Assert
        Assert.Equal(expectedClientId, clientId);
        mockCognitoIdentityProvider.Verify(c => c.CreateUserPoolClientAsync(
            It.Is<CreateUserPoolClientRequest>(req => 
                req.UserPoolId == userPoolId && 
                req.ClientName == expectedClientName),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GivenEmailAndUserPoolId_WhenCreateUserAsync_ThenCreatesUserAndReturnsTrue()
    {
        // Arrange
        var mockCognitoIdentity = new Mock<IAmazonCognitoIdentity>();
        var mockCognitoIdentityProvider = new Mock<IAmazonCognitoIdentityProvider>();
        var options = new AwsServiceOptions
        {
            ResourcePrefix = "test-prefix"
        };
        var sut = new CognitoService(
            mockCognitoIdentity.Object,
            mockCognitoIdentityProvider.Object,
            options,
            Mock.Of<ILogger<CognitoService>>());
        var email = "test@example.com";
        var userPoolId = "us-east-1_ABC123DEF";

        mockCognitoIdentityProvider
            .Setup(c => c.AdminCreateUserAsync(
                It.Is<AdminCreateUserRequest>(req => 
                    req.UserPoolId == userPoolId && 
                    req.Username == email &&
                    req.UserAttributes.Any(attr => attr.Name == "email" && attr.Value == email) &&
                    req.DesiredDeliveryMediums.Contains("EMAIL")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AdminCreateUserResponse
            {
                HttpStatusCode = System.Net.HttpStatusCode.OK
            });
        
        // Act
        var result = await sut.CreateUserAsync(email, userPoolId);

        // Assert
        Assert.True(result);
        mockCognitoIdentityProvider.Verify(c => c.AdminCreateUserAsync(
            It.Is<AdminCreateUserRequest>(req => 
                req.UserPoolId == userPoolId && 
                req.Username == email &&
                req.UserAttributes.Any(attr => attr.Name == "email" && attr.Value == email) &&
                req.DesiredDeliveryMediums.Contains("EMAIL")),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
