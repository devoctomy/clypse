using Amazon.CognitoIdentity;
using Amazon.CognitoIdentity.Model;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.IdentityManagement.Model;
using clypse.portal.setup.Services.Cognito;
using Microsoft.Extensions.Logging;
using Moq;

namespace clypse.portal.setup.UnitTests.Services.Cognito;

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

        var tags = new Dictionary<string, string>
        {
            { "Hello", "World" },
            { "Foo", "Bar" }
        };

        mockCognitoIdentity
            .Setup(c => c.CreateIdentityPoolAsync(
                It.Is<CreateIdentityPoolRequest>(req =>
                    req.IdentityPoolName == expecvtedIdentityPoolName &&
                    req.IdentityPoolTags == tags),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateIdentityPoolResponse
            {
                IdentityPoolId = expectedIdentityPoolId
            });
        
        // Act
        var identityPoolId = await sut.CreateIdentityPoolAsync(identityPoolName, tags);

        // Assert
        Assert.Equal(expectedIdentityPoolId, identityPoolId);
        mockCognitoIdentity.Verify(c => c.CreateIdentityPoolAsync(
            It.Is<CreateIdentityPoolRequest>(req =>
                req.IdentityPoolName == expecvtedIdentityPoolName &&
                req.IdentityPoolTags == tags),
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

        var tags = new Dictionary<string, string>
        {
            { "Hello", "World" },
            { "Foo", "Bar" }
        };

        mockCognitoIdentityProvider
            .Setup(c => c.CreateUserPoolAsync(
                It.Is<CreateUserPoolRequest>(req =>
                    req.PoolName == expecteduserPoolName &&
                    req.UserPoolTags == tags),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateUserPoolResponse
            {
                UserPool = new UserPoolType
                {
                    Id = expectedUserPoolId
                }
            });
        
        // Act
        var userPoolId = await sut.CreateUserPoolAsync(userPoolName, tags);

        // Assert
        Assert.Equal(expectedUserPoolId, userPoolId);
        mockCognitoIdentityProvider.Verify(c => c.CreateUserPoolAsync(
            It.Is<CreateUserPoolRequest>(req =>
                req.PoolName == expecteduserPoolName &&
                req.UserPoolTags == tags),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GivenUserPoolIdAndName_WhenCreateUserPoolClient_ThenCreatesUserPoolClient_AndTagsResource_AndReturnsClientId()
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
        var accountId = "123456789012";
        var userPoolId = "us-east-1_ABC123DEF";
        var clientName = "test-client";
        var expectedClientName = "test-prefix.test-client";
        var expectedClientId = "1234567890abcdef1234567890";
        var clientArn = $"arn:aws:cognito-idp:{options.Region}:{accountId}:userpool/{userPoolId}/client/{expectedClientId}";

        var tags = new Dictionary<string, string>
        {
            { "Hello", "World" },
            { "Foo", "Bar" }
        };

        mockCognitoIdentityProvider
            .Setup(c => c.CreateUserPoolClientAsync(
                It.Is<CreateUserPoolClientRequest>(req => 
                    req.UserPoolId == userPoolId && 
                    req.ClientName == expectedClientName),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateUserPoolClientResponse
            {
                HttpStatusCode = System.Net.HttpStatusCode.OK,
                UserPoolClient = new UserPoolClientType
                {
                    ClientId = expectedClientId
                }
            });

        mockCognitoIdentityProvider
            .Setup(x => x.TagResourceAsync(
                It.Is<Amazon.CognitoIdentityProvider.Model.TagResourceRequest>(req =>
                    req.ResourceArn == clientArn &&
                    req.Tags == tags),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Amazon.CognitoIdentityProvider.Model.TagResourceResponse
            {
                HttpStatusCode = System.Net.HttpStatusCode.OK
            });

        // Act
        var clientId = await sut.CreateUserPoolClientAsync(accountId, clientName, userPoolId, tags);

        // Assert
        Assert.Equal(expectedClientId, clientId);
        mockCognitoIdentityProvider.Verify(c => c.CreateUserPoolClientAsync(
            It.Is<CreateUserPoolClientRequest>(req => 
                req.UserPoolId == userPoolId && 
                req.ClientName == expectedClientName),
            It.IsAny<CancellationToken>()),
            Times.Once);
        mockCognitoIdentityProvider
            .Verify(x => x.TagResourceAsync(
                It.Is<Amazon.CognitoIdentityProvider.Model.TagResourceRequest>(req =>
                    req.ResourceArn == clientArn &&
                    req.Tags == tags),
                It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenUserPoolIdAndName_WhenCreateUserPoolClient_ThenFailsToCreateUserPoolClient_AndReturnsNull()
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
        var accountId = "123456789012";
        var userPoolId = "us-east-1_ABC123DEF";
        var clientName = "test-client";
        var expectedClientName = "test-prefix.test-client";

        var tags = new Dictionary<string, string>
        {
            { "Hello", "World" },
            { "Foo", "Bar" }
        };

        mockCognitoIdentityProvider
            .Setup(c => c.CreateUserPoolClientAsync(
                It.Is<CreateUserPoolClientRequest>(req =>
                    req.UserPoolId == userPoolId &&
                    req.ClientName == expectedClientName),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateUserPoolClientResponse
            {
                HttpStatusCode = System.Net.HttpStatusCode.InternalServerError
            });

        // Act
        var clientId = await sut.CreateUserPoolClientAsync(accountId, clientName, userPoolId, tags);

        // Assert
        Assert.Null(clientId);
        mockCognitoIdentityProvider.Verify(c => c.CreateUserPoolClientAsync(
            It.Is<CreateUserPoolClientRequest>(req =>
                req.UserPoolId == userPoolId &&
                req.ClientName == expectedClientName),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GivenEmailAndUserPoolId_WhenCreateUserAsync_ThenCreatesUser_AndTagsUser_AndReturnsTrue()
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
        var userId = Guid.NewGuid().ToString();
        var email = "test@example.com";
        var userPoolId = "us-east-1_ABC123DEF";

        var tags = new Dictionary<string, string>
        {
            { "Hello", "World" },
            { "Foo", "Bar" }
        };

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
                HttpStatusCode = System.Net.HttpStatusCode.OK,
                User = new UserType
                {
                    Username = userId
                }
            });

        mockCognitoIdentityProvider
            .Setup(x => x.TagResourceAsync(
                It.Is<Amazon.CognitoIdentityProvider.Model.TagResourceRequest>(req =>
                    req.ResourceArn == userId &&
                    req.Tags == tags),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Amazon.CognitoIdentityProvider.Model.TagResourceResponse
            {
                HttpStatusCode = System.Net.HttpStatusCode.OK
            });

        // Act
        var result = await sut.CreateUserAsync(email, userPoolId, tags);

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
        mockCognitoIdentityProvider
            .Verify(x => x.TagResourceAsync(
                It.Is<Amazon.CognitoIdentityProvider.Model.TagResourceRequest>(req =>
                    req.ResourceArn == userId &&
                    req.Tags == tags),
                It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenEmailAndUserPoolId_WhenCreateUserAsync_ThenFailsToCreateUser_AndReturnsFalse()
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
        var userId = Guid.NewGuid().ToString();
        var email = "test@example.com";
        var userPoolId = "us-east-1_ABC123DEF";

        var tags = new Dictionary<string, string>
        {
            { "Hello", "World" },
            { "Foo", "Bar" }
        };

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
                HttpStatusCode = System.Net.HttpStatusCode.InternalServerError,
            });

        // Act
        var result = await sut.CreateUserAsync(email, userPoolId, tags);

        // Assert
        Assert.False(result);
        mockCognitoIdentityProvider.Verify(c => c.AdminCreateUserAsync(
            It.Is<AdminCreateUserRequest>(req =>
                req.UserPoolId == userPoolId &&
                req.Username == email &&
                req.UserAttributes.Any(attr => attr.Name == "email" && attr.Value == email) &&
                req.DesiredDeliveryMediums.Contains("EMAIL")),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GivenIdentityPoolIdAndRoleArn_WhenSetIdentityPoolAuthenticatedRoleAsync_ThenSetsRoleAndReturnsTrue()
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
        var identityPoolId = "us-east-1:12345678-1234-1234-1234-123456789012";
        var roleArn = "arn:aws:iam::123456789012:role/test-role";

        mockCognitoIdentity
            .Setup(c => c.SetIdentityPoolRolesAsync(
                It.Is<SetIdentityPoolRolesRequest>(req => 
                    req.IdentityPoolId == identityPoolId && 
                    req.Roles.ContainsKey("authenticated") &&
                    req.Roles["authenticated"] == roleArn),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SetIdentityPoolRolesResponse
            {
                HttpStatusCode = System.Net.HttpStatusCode.OK
            });
        
        // Act
        var result = await sut.SetIdentityPoolAuthenticatedRoleAsync(identityPoolId, roleArn);

        // Assert
        Assert.True(result);
        mockCognitoIdentity.Verify(c => c.SetIdentityPoolRolesAsync(
            It.Is<SetIdentityPoolRolesRequest>(req => 
                req.IdentityPoolId == identityPoolId && 
                req.Roles.ContainsKey("authenticated") &&
                req.Roles["authenticated"] == roleArn),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GivenIdentityPoolIdAndRoleArn_WhenSetIdentityPoolAuthenticatedRoleAsyncFails_ThenReturnsFalse()
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
        var identityPoolId = "us-east-1:12345678-1234-1234-1234-123456789012";
        var roleArn = "arn:aws:iam::123456789012:role/test-role";

        mockCognitoIdentity
            .Setup(c => c.SetIdentityPoolRolesAsync(
                It.Is<SetIdentityPoolRolesRequest>(req => 
                    req.IdentityPoolId == identityPoolId && 
                    req.Roles.ContainsKey("authenticated") &&
                    req.Roles["authenticated"] == roleArn),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SetIdentityPoolRolesResponse
            {
                HttpStatusCode = System.Net.HttpStatusCode.BadRequest
            });
        
        // Act
        var result = await sut.SetIdentityPoolAuthenticatedRoleAsync(identityPoolId, roleArn);

        // Assert
        Assert.False(result);
        mockCognitoIdentity.Verify(c => c.SetIdentityPoolRolesAsync(
            It.Is<SetIdentityPoolRolesRequest>(req => 
                req.IdentityPoolId == identityPoolId && 
                req.Roles.ContainsKey("authenticated") &&
                req.Roles["authenticated"] == roleArn),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
