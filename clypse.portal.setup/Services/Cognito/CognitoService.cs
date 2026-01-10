using Amazon.CognitoIdentity;
using Amazon.CognitoIdentity.Model;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Microsoft.Extensions.Logging;

namespace clypse.portal.setup.Services.Cognito;

/// <summary>
/// Provides functionality for managing AWS Cognito identity pools, user pools, and users.
/// </summary>
public class CognitoService(
    IAmazonCognitoIdentity amazonCognitoIdentity,
    IAmazonCognitoIdentityProvider amazonCognitoIdentityProvider,
    AwsServiceOptions options,
    ILogger<CognitoService> logger) : ICognitoService
{
    /// <summary>
    /// Creates a new Cognito identity pool with the specified name.
    /// </summary>
    /// <param name="name">The name of the identity pool to create (without the resource prefix).</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The ID of the created identity pool.</returns>
    public async Task<string> CreateIdentityPoolAsync(
        string name,
        Dictionary<string, string> tags,
        CancellationToken cancellationToken = default)
    {
        var identityPoolNameWithPrefix = $"{options.ResourcePrefix}.{name}";
        logger.LogInformation("Creating Identity Pool: {identityPoolNameWithPrefix}", identityPoolNameWithPrefix);

        var createIdentityPool = new CreateIdentityPoolRequest
        {
            IdentityPoolName = identityPoolNameWithPrefix,
            IdentityPoolTags = tags
        };
        var response = await amazonCognitoIdentity.CreateIdentityPoolAsync(createIdentityPool, cancellationToken);
        return response.IdentityPoolId;
    }

    /// <summary>
    /// Creates a new user in the specified Cognito user pool.
    /// </summary>
    /// <param name="email">The email address of the user to create.</param>
    /// <param name="userPoolId">The ID of the user pool where the user will be created.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>True if the user was created successfully; otherwise, false.</returns>
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

        var tagUserResponse = await amazonCognitoIdentityProvider.TagResourceAsync(new Amazon.CognitoIdentityProvider.Model.TagResourceRequest
        {
            ResourceArn = response.User.Username,
            Tags = tags
        }, cancellationToken);

        return tagUserResponse.HttpStatusCode == System.Net.HttpStatusCode.OK;
    }

    /// <summary>
    /// Creates a new Cognito user pool with the specified name.
    /// </summary>
    /// <param name="name">The name of the user pool to create (without the resource prefix).</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The ID of the created user pool.</returns>
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

    /// <summary>
    /// Creates a new client for the specified Cognito user pool.
    /// </summary>
    /// <param name="accountId">The AWS account ID.</param>
    /// <param name="name">The name of the user pool client to create (without the resource prefix).</param>
    /// <param name="userPoolId">The ID of the user pool where the client will be created.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The client ID of the created user pool client.</returns>
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

        var clientArn = $"arn:aws:cognito-idp:{options.Region}:{accountId}:userpool/{userPoolId}/client/{createUserPoolClientResponse.UserPoolClient.ClientId}";
        var tagUserResponse = await amazonCognitoIdentityProvider.TagResourceAsync(new Amazon.CognitoIdentityProvider.Model.TagResourceRequest
        {
            ResourceArn = clientArn,
            Tags = tags
        }, cancellationToken);

        if (tagUserResponse.HttpStatusCode != System.Net.HttpStatusCode.OK)
        {
            logger.LogError(
                "Failed to tag user pool client {clientArn} in user pool {userPoolId}. HTTP Status Code: {statusCode}",
                clientArn,
                userPoolId,
                tagUserResponse.HttpStatusCode);
            return null;
        }

        return createUserPoolClientResponse.UserPoolClient.ClientId;
    }

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
