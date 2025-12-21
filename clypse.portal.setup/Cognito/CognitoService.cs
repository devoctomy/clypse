using Amazon.CognitoIdentity;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Microsoft.Extensions.Logging;

namespace clypse.portal.setup.Cognito;

public class CognitoService(
    IAmazonCognitoIdentity amazonCognitoIdentity,
    IAmazonCognitoIdentityProvider amazonCognitoIdentityProvider,
    AwsServiceOptions options,
    ILogger<CognitoService> logger) : ICognitoService
{
    public async Task<string> CreateIdentityPoolAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        var identityPoolNameWithPrefix = $"{options.ResourcePrefix}.{name}";
        logger.LogInformation("Creating Identity Pool: {identityPoolNameWithPrefix}", identityPoolNameWithPrefix);

        var createIdentityPool = new Amazon.CognitoIdentity.Model.CreateIdentityPoolRequest
        {
            IdentityPoolName = identityPoolNameWithPrefix
        };
        var response = await amazonCognitoIdentity.CreateIdentityPoolAsync(createIdentityPool, cancellationToken);
        return response.IdentityPoolId;
    }

    public async Task<bool> CreateUserAsync(
        string email,
        string userPoolId,
        CancellationToken cancellationToken = default)
    {
        var adminCreateUserRequest = new AdminCreateUserRequest
        {
            UserPoolId = userPoolId,
            Username = email,
            UserAttributes =
            [
                new() {
                    Name = "email",
                    Value = email
                }
            ],
            DesiredDeliveryMediums =
            [
                "EMAIL"
            ]
        };

        var response = await amazonCognitoIdentityProvider.AdminCreateUserAsync(
            adminCreateUserRequest,
            cancellationToken);
        return
            response.HttpStatusCode == System.Net.HttpStatusCode.OK ||
            response.HttpStatusCode == System.Net.HttpStatusCode.Created;
    }

    public async Task<string> CreateUserPoolAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        var userPoolNameWithPrefix = $"{options.ResourcePrefix}.{name}";
        logger.LogInformation("Creating User Pool: {userPoolNameWithPrefix}", userPoolNameWithPrefix);

        var createUserPoolRequest = new CreateUserPoolRequest
        {
            PoolName = userPoolNameWithPrefix,
        };
        var response = await amazonCognitoIdentityProvider.CreateUserPoolAsync(createUserPoolRequest, cancellationToken);
        return response.UserPool.Id;
    }

    public async Task<string> CreateUserPoolClientAsync(
        string name,
        string userPoolId,
        CancellationToken cancellationToken = default)
    {
        var userPoolClientNameWithPrefix = $"{options.ResourcePrefix}.{name}";
        logger.LogInformation("Creating User Pool Client: {userPoolClientNameWithPrefix}", userPoolClientNameWithPrefix);

        var createUserPoolClientRequest = new CreateUserPoolClientRequest
        {
            ClientName = userPoolClientNameWithPrefix,
            UserPoolId = userPoolId,
        };
        var response = await amazonCognitoIdentityProvider.CreateUserPoolClientAsync(createUserPoolClientRequest, cancellationToken);
        return response.UserPoolClient.ClientId;
    }
}
