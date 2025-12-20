using Amazon.CognitoIdentity;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;

namespace clypse.portal.setup.Cognito;

public class CognitoService(
    IAmazonCognitoIdentity amazonCognitoIdentity,
    IAmazonCognitoIdentityProvider amazonCognitoIdentityProvider) : ICognitoService
{
    public async Task<string> CreateIdentityPoolAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        var createIdentityPool = new Amazon.CognitoIdentity.Model.CreateIdentityPoolRequest
        {
            IdentityPoolName = name
        };
        var response = await amazonCognitoIdentity.CreateIdentityPoolAsync(createIdentityPool, cancellationToken);
        return response.IdentityPoolId;
    }

    public async Task<string> CreateUserPoolAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        var createUserPoolRequest = new CreateUserPoolRequest
        {
            PoolName = name,
        };
        var response = await amazonCognitoIdentityProvider.CreateUserPoolAsync(createUserPoolRequest, cancellationToken);
        return response.UserPool.Id;
    }

    public async Task<string> CreateUserPoolClientAsync(
        string userPoolId,
        string name,
        CancellationToken cancellationToken = default)
    {
        var createUserPoolClientRequest = new CreateUserPoolClientRequest
        {
            UserPoolId = userPoolId,
            ClientName = name,
        };
        var response = await amazonCognitoIdentityProvider.CreateUserPoolClientAsync(createUserPoolClientRequest, cancellationToken);
        return response.UserPoolClient.ClientId;
    }
}
