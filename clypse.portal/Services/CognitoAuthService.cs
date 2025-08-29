using Amazon;
using Amazon.CognitoIdentity;
using Amazon.CognitoIdentity.Model;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using clypse.portal.Models;

namespace clypse.portal.Services;

public class CognitoAuthService : ICognitoAuthService
{
    private readonly AwsCognitoConfig _config;
    private readonly AmazonCognitoIdentityProviderClient _cognitoClient;
    private readonly AmazonCognitoIdentityClient _identityClient;
    
    private string? _accessToken;
    private string? _idToken;
    private string? _refreshToken;

    public CognitoAuthService(AwsCognitoConfig config)
    {
        _config = config;
        var region = RegionEndpoint.GetBySystemName(_config.Region);
        _cognitoClient = new AmazonCognitoIdentityProviderClient(region);
        _identityClient = new AmazonCognitoIdentityClient(region);
    }

    public bool IsAuthenticated => !string.IsNullOrEmpty(_accessToken);
    public string? AccessToken => _accessToken;
    public string? IdToken => _idToken;

    public async Task<LoginResult> LoginAsync(string username, string password)
    {
        try
        {
            var authRequest = new InitiateAuthRequest
            {
                AuthFlow = AuthFlowType.USER_PASSWORD_AUTH,
                ClientId = _config.UserPoolClientId,
                AuthParameters = new Dictionary<string, string>
                {
                    { "USERNAME", username },
                    { "PASSWORD", password }
                }
            };

            var authResponse = await _cognitoClient.InitiateAuthAsync(authRequest);

            if (authResponse.AuthenticationResult != null)
            {
                _accessToken = authResponse.AuthenticationResult.AccessToken;
                _idToken = authResponse.AuthenticationResult.IdToken;
                _refreshToken = authResponse.AuthenticationResult.RefreshToken;

                // Get AWS credentials using the ID token
                var awsCredentials = await GetAwsCredentialsAsync(_idToken);

                return new LoginResult
                {
                    Success = true,
                    AccessToken = _accessToken,
                    IdToken = _idToken,
                    RefreshToken = _refreshToken,
                    AwsCredentials = awsCredentials
                };
            }
            else
            {
                return new LoginResult
                {
                    Success = false,
                    ErrorMessage = "Authentication failed"
                };
            }
        }
        catch (Exception ex)
        {
            return new LoginResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public Task LogoutAsync()
    {
        _accessToken = null;
        _idToken = null;
        _refreshToken = null;
        return Task.CompletedTask;
    }

    private async Task<Credentials?> GetAwsCredentialsAsync(string idToken)
    {
        try
        {
            var getIdRequest = new GetIdRequest
            {
                IdentityPoolId = _config.IdentityPoolId,
                Logins = new Dictionary<string, string>
                {
                    { $"cognito-idp.{_config.Region}.amazonaws.com/{_config.UserPoolId}", idToken }
                }
            };

            var getIdResponse = await _identityClient.GetIdAsync(getIdRequest);

            var getCredentialsRequest = new GetCredentialsForIdentityRequest
            {
                IdentityId = getIdResponse.IdentityId,
                Logins = new Dictionary<string, string>
                {
                    { $"cognito-idp.{_config.Region}.amazonaws.com/{_config.UserPoolId}", idToken }
                }
            };

            var credentialsResponse = await _identityClient.GetCredentialsForIdentityAsync(getCredentialsRequest);
            return credentialsResponse.Credentials;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
