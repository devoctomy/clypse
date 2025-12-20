using Amazon.CognitoIdentityProvider.Model;

namespace clypse.portal.setup.Cognito;

public interface ICognitoService
{
    public Task<string> CreateIdentityPoolAsync(
        string name,
        CancellationToken cancellationToken = default);

    public Task<string> CreateUserPoolAsync(
        string name,
        CancellationToken cancellationToken = default);

    public Task<string> CreateUserPoolClientAsync(
        string name,
        string userPoolId,
        CancellationToken cancellationToken = default);
}
