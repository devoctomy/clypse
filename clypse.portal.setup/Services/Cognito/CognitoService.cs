using Amazon.CognitoIdentity;
using Amazon.CognitoIdentity.Model;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Microsoft.Extensions.Logging;

namespace clypse.portal.setup.Services.Cognito;

/// <inheritdoc cref="ICognitoService" />
public class CognitoService(
    IAmazonCognitoIdentity amazonCognitoIdentity,
    IAmazonCognitoIdentityProvider amazonCognitoIdentityProvider,
    SetupOptions options,
    ILogger<CognitoService> logger) : ICognitoService
{
    /// <inheritdoc />
    public async Task<string> CreateIdentityPoolAsync(
        string name,
        string userPoolId,
        string userPoolClientId,
        Dictionary<string, string> tags,
        CancellationToken cancellationToken = default)
    {
        var identityPoolNameWithPrefix = $"{options.ResourcePrefix}.{name}";
        logger.LogInformation("Creating Identity Pool: {identityPoolNameWithPrefix}", identityPoolNameWithPrefix);

        var createIdentityPool = new CreateIdentityPoolRequest
        {
            IdentityPoolName = identityPoolNameWithPrefix,
            IdentityPoolTags = tags,
            CognitoIdentityProviders =
            [
                new CognitoIdentityProviderInfo
                {
                    ProviderName = $"cognito-idp.{options.Region}.amazonaws.com/{userPoolId}",
                    ClientId = userPoolClientId,
                    ServerSideTokenCheck = false
                }
            ],
        };
        var response = await amazonCognitoIdentity.CreateIdentityPoolAsync(createIdentityPool, cancellationToken);
        return response.IdentityPoolId;
    }

    /// <inheritdoc />
    public async Task<bool> CreateUserAsync(
        string email,
        string userPoolId,
        Dictionary<string, string> tags,
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

        if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
        {
            logger.LogError(
                "Failed to create user {email} in user pool {userPoolId}. HTTP Status Code: {statusCode}",
                email,
                userPoolId,
                response.HttpStatusCode);
            return false;
        }

        // TODO: Need to fix this
        ////var tagUserResponse = await amazonCognitoIdentityProvider.TagResourceAsync(new Amazon.CognitoIdentityProvider.Model.TagResourceRequest
        ////{
        ////    ResourceArn = response.User.Username,
        ////    Tags = tags
        ////}, cancellationToken);

        ////return tagUserResponse.HttpStatusCode == System.Net.HttpStatusCode.OK;
        ///

        return true;
    }

    /// <inheritdoc />
    public async Task<string> CreateUserPoolAsync(
        string name,
        Dictionary<string, string> tags,
        CancellationToken cancellationToken = default)
    {
        var userPoolNameWithPrefix = $"{options.ResourcePrefix}.{name}";
        logger.LogInformation("Creating User Pool: {userPoolNameWithPrefix}", userPoolNameWithPrefix);

        var createUserPoolRequest = new CreateUserPoolRequest
        {
            PoolName = userPoolNameWithPrefix,
            UserPoolTags = tags
        };
        var response = await amazonCognitoIdentityProvider.CreateUserPoolAsync(createUserPoolRequest, cancellationToken);
        
        return response.UserPool.Id;
    }

    /// <inheritdoc />
    public async Task<string?> CreateUserPoolClientAsync(
        string accountId,
        string name,
        string userPoolId,
        Dictionary<string, string> tags,
        CancellationToken cancellationToken = default)
    {
        var userPoolClientNameWithPrefix = $"{options.ResourcePrefix}.{name}";
        logger.LogInformation("Creating User Pool Client: {userPoolClientNameWithPrefix}", userPoolClientNameWithPrefix);

        var createUserPoolClientRequest = new CreateUserPoolClientRequest
        {
            ClientName = userPoolClientNameWithPrefix,
            UserPoolId = userPoolId,
        };

        var createUserPoolClientResponse = await amazonCognitoIdentityProvider.CreateUserPoolClientAsync(createUserPoolClientRequest, cancellationToken);
        if (createUserPoolClientResponse.HttpStatusCode != System.Net.HttpStatusCode.OK)
        {
            logger.LogError(
                "Failed to create user pool client {userPoolClientNameWithPrefix} in user pool {userPoolId}. HTTP Status Code: {statusCode}",
                userPoolClientNameWithPrefix,
                userPoolId,
                createUserPoolClientResponse.HttpStatusCode);
            return null;
        }

        // TODO: Need to fix this
        ////var clientArn = $"arn:aws:cognito-idp:{options.Region}:{accountId}:userpool/{userPoolId}/client/{createUserPoolClientResponse.UserPoolClient.ClientId}";
        ////var tagUserResponse = await amazonCognitoIdentityProvider.TagResourceAsync(new Amazon.CognitoIdentityProvider.Model.TagResourceRequest
        ////{
        ////    ResourceArn = clientArn,
        ////    Tags = tags
        ////}, cancellationToken);

        ////if (tagUserResponse.HttpStatusCode != System.Net.HttpStatusCode.OK)
        ////{
        ////    logger.LogError(
        ////        "Failed to tag user pool client {clientArn} in user pool {userPoolId}. HTTP Status Code: {statusCode}",
        ////        clientArn,
        ////        userPoolId,
        ////        tagUserResponse.HttpStatusCode);
        ////    return null;
        ////}

        return createUserPoolClientResponse.UserPoolClient.ClientId;
    }

    /// <inheritdoc />
    public async Task<bool> SetIdentityPoolAuthenticatedRoleAsync(
        string identityPoolId,
        string roleArn,
        CancellationToken cancellationToken = default)
    {
        var setIdentityPoolRolesRequest = new SetIdentityPoolRolesRequest
        {
            IdentityPoolId = identityPoolId,
            Roles = new Dictionary<string, string>
            {
                { "authenticated", roleArn }
            }
        };

        var response = await amazonCognitoIdentity.SetIdentityPoolRolesAsync(setIdentityPoolRolesRequest, cancellationToken);
        return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
    }
}
