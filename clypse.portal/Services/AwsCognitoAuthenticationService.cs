using System.Text.Json;
using clypse.portal.Models;
using Microsoft.JSInterop;

namespace clypse.portal.Services;

public class AwsCognitoAuthenticationService : IAuthenticationService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly AwsCognitoConfig _cognitoConfig;
    private bool _isInitialized;

    public AwsCognitoAuthenticationService(IJSRuntime jsRuntime, AwsCognitoConfig cognitoConfig)
    {
        _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
        _cognitoConfig = cognitoConfig ?? throw new ArgumentNullException(nameof(cognitoConfig));
    }

    public async Task Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        // Initialize Cognito configuration in JavaScript
        await _jsRuntime.InvokeVoidAsync("eval", $@"
            window.cognitoConfig = {{
                userPoolId: '{_cognitoConfig.UserPoolId}',
                userPoolClientId: '{_cognitoConfig.UserPoolClientId}',
                region: '{_cognitoConfig.Region}',
                identityPoolId: '{_cognitoConfig.IdentityPoolId}'
            }};
        ");

        await _jsRuntime.InvokeAsync<string>("CognitoAuth.initialize", _cognitoConfig);
        _isInitialized = true;
    }

    public async Task<bool> CheckAuthentication()
    {
        try
        {
            var credentials = await GetStoredCredentials();
            if (credentials == null)
            {
                return false;
            }

            // Check if credentials are expired
            if (DateTime.TryParse(credentials.ExpirationTime, out var expirationTime))
            {
                if (expirationTime <= DateTime.UtcNow)
                {
                    await ClearStoredCredentials();
                    return false;
                }
            }

            // Verify the credentials are still valid with Cognito
            if (!string.IsNullOrEmpty(credentials.IdToken))
            {
                try
                {
                    var freshAwsCredentials = await _jsRuntime.InvokeAsync<AwsCredentials>("CognitoAuth.getAwsCredentials", credentials.IdToken);
                    if (freshAwsCredentials != null)
                    {
                        freshAwsCredentials.IdentityId = credentials.AwsCredentials?.IdentityId;                        
                        credentials.AwsCredentials = freshAwsCredentials;
                        var credentialsJson = JsonSerializer.Serialize(credentials);
                        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "clypse_credentials", credentialsJson);
                    }
                    
                    return true;
                }
                catch
                {
                    await ClearStoredCredentials();
                    return false;
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    public async Task<LoginResult> Login(string username, string password)
    {
        try
        {
            var result = await _jsRuntime.InvokeAsync<LoginResult>("CognitoAuth.login", username, password);

            if (result.Success)
            {
                await StoreCredentials(result);
            }

            return result;
        }
        catch (Exception ex)
        {
            return new LoginResult
            {
                Success = false,
                Error = $"An error occurred during login: {ex.Message}"
            };
        }
    }

    public async Task Logout()
    {
        await _jsRuntime.InvokeVoidAsync("CognitoAuth.logout");
        await ClearStoredCredentials();
    }

    public async Task<StoredCredentials?> GetStoredCredentials()
    {
        try
        {
            var credentialsJson = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "clypse_credentials");
            
            if (string.IsNullOrEmpty(credentialsJson))
            {
                return null;
            }

            return JsonSerializer.Deserialize<StoredCredentials>(credentialsJson);
        }
        catch
        {
            return null;
        }
    }

    private async Task StoreCredentials(LoginResult result)
    {
        var credentials = new StoredCredentials
        {
            AccessToken = result.AccessToken,
            IdToken = result.IdToken,
            AwsCredentials = result.AwsCredentials,
            ExpirationTime = result.AwsCredentials?.Expiration ?? DateTime.UtcNow.AddHours(1).ToString(),
            StoredAt = DateTime.UtcNow
        };

        var credentialsJson = JsonSerializer.Serialize(credentials);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "clypse_credentials", credentialsJson);
    }

    private async Task ClearStoredCredentials()
    {
        // Clear ALL localStorage data
        await _jsRuntime.InvokeVoidAsync("localStorage.clear");
    }
}
