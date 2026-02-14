using System.Text.Json;
using clypse.portal.Models;
using clypse.portal.Models.Aws;
using clypse.portal.Models.Login;
using Microsoft.JSInterop;

namespace clypse.portal.Services;

public class AwsCognitoAuthenticationService : IAuthenticationService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly AwsCognitoConfig _cognitoConfig;
    private readonly ILocalStorageService _localStorage;
    private bool _isInitialized;
    private bool _justLoggedIn;

    public AwsCognitoAuthenticationService(IJSRuntime jsRuntime, AwsCognitoConfig cognitoConfig, ILocalStorageService localStorage)
    {
        _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
        _cognitoConfig = cognitoConfig ?? throw new ArgumentNullException(nameof(cognitoConfig));
        _localStorage = localStorage ?? throw new ArgumentNullException(nameof(localStorage));
    }

    public async Task Initialize()
    {
        if (_isInitialized)
        {
            Console.WriteLine("AwsCognitoAuthenticationService.Initialize: Already initialized, skipping");
            return;
        }

        Console.WriteLine("AwsCognitoAuthenticationService.Initialize: Starting initialization");

        // Initialize Cognito configuration in JavaScript
        await _jsRuntime.InvokeVoidAsync("eval", $@"
            window.cognitoConfig = {{
                userPoolId: '{_cognitoConfig.UserPoolId}',
                userPoolClientId: '{_cognitoConfig.UserPoolClientId}',
                region: '{_cognitoConfig.Region}',
                identityPoolId: '{_cognitoConfig.IdentityPoolId}'
            }};
        ");

        var initResult = await _jsRuntime.InvokeAsync<string>("CognitoAuth.initialize", _cognitoConfig);        
        _isInitialized = true;
        Console.WriteLine("AwsCognitoAuthenticationService.Initialize: Initialization complete");
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

            // If we just logged in, skip the refresh to avoid double AWS API calls
            if (_justLoggedIn)
            {
                Console.WriteLine("AwsCognitoAuthenticationService.CheckAuthentication: Just logged in, skipping credential refresh");
                _justLoggedIn = false; // Reset the flag
                return true;
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
                    Console.WriteLine("AwsCognitoAuthenticationService.CheckAuthentication: Refreshing AWS credentials");
                    var freshAwsCredentials = await _jsRuntime.InvokeAsync<AwsCredentials>("CognitoAuth.getAwsCredentials", credentials.IdToken);
                    if (freshAwsCredentials != null)
                    {
                        freshAwsCredentials.IdentityId = credentials.AwsCredentials?.IdentityId ?? string.Empty;                        
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
            Console.WriteLine($"AwsCognitoAuthenticationService.Login: Starting login for user: {username}");
            var result = await _jsRuntime.InvokeAsync<LoginResult>("CognitoAuth.login", username, password);

            Console.WriteLine($"AwsCognitoAuthenticationService.Login: Login result success: {result.Success}");
            if (!string.IsNullOrEmpty(result.Error))
            {
                Console.WriteLine($"AwsCognitoAuthenticationService.Login: Error: {result.Error}");
            }
            
            if (result.AwsCredentials != null)
            {
                Console.WriteLine($"AwsCognitoAuthenticationService.Login: AWS Credentials received.");
            }
            else
            {
                Console.WriteLine("AwsCognitoAuthenticationService.Login: No AWS Credentials received");
            }

            if (result.Success)
            {
                Console.WriteLine("AwsCognitoAuthenticationService.Login: Storing credentials");
                await StoreCredentials(result);
                _justLoggedIn = true; // Set flag to avoid double credential refresh
                Console.WriteLine("AwsCognitoAuthenticationService.Login: Set _justLoggedIn flag to prevent double AWS API calls");
            }

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"AwsCognitoAuthenticationService.Login: Exception: {ex.Message}");
            Console.WriteLine($"AwsCognitoAuthenticationService.Login: Stack trace: {ex.StackTrace}");
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
        Console.WriteLine("AwsCognitoAuthenticationService.StoreCredentials: Storing credentials to localStorage");
        
        var credentials = new StoredCredentials
        {
            AccessToken = result.AccessToken,
            IdToken = result.IdToken,
            AwsCredentials = result.AwsCredentials,
            ExpirationTime = result.AwsCredentials?.Expiration ?? DateTime.UtcNow.AddHours(1).ToString(),
            StoredAt = DateTime.UtcNow
        };

        if (result.AwsCredentials != null)
        {
            Console.WriteLine($"AwsCognitoAuthenticationService.StoreCredentials: Storing AWS Credentials with IdentityId: '{result.AwsCredentials.IdentityId}'");
        }

        var credentialsJson = JsonSerializer.Serialize(credentials);
        Console.WriteLine($"AwsCognitoAuthenticationService.StoreCredentials: Serialized credentials length: {credentialsJson.Length}");
        
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "clypse_credentials", credentialsJson);
        Console.WriteLine("AwsCognitoAuthenticationService.StoreCredentials: Credentials stored successfully");
    }

    public async Task<LoginResult> CompletePasswordReset(string username, string newPassword)
    {
        try
        {
            Console.WriteLine($"AwsCognitoAuthenticationService.CompletePasswordReset: Starting password reset for user: {username}");
            var result = await _jsRuntime.InvokeAsync<LoginResult>("CognitoAuth.completePasswordReset", username, newPassword);

            Console.WriteLine($"AwsCognitoAuthenticationService.CompletePasswordReset: Password reset result success: {result.Success}");
            if (!string.IsNullOrEmpty(result.Error))
            {
                Console.WriteLine($"AwsCognitoAuthenticationService.CompletePasswordReset: Error: {result.Error}");
            }

            if (result.Success)
            {
                Console.WriteLine("AwsCognitoAuthenticationService.CompletePasswordReset: Storing credentials");
                await StoreCredentials(result);
                _justLoggedIn = true;
                Console.WriteLine("AwsCognitoAuthenticationService.CompletePasswordReset: Set _justLoggedIn flag to prevent double AWS API calls");
            }

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"AwsCognitoAuthenticationService.CompletePasswordReset: Exception: {ex.Message}");
            return new LoginResult
            {
                Success = false,
                Error = $"An error occurred during password reset: {ex.Message}"
            };
        }
    }

    public async Task<ForgotPasswordResult> ForgotPassword(string username)
    {
        try
        {
            Console.WriteLine($"AwsCognitoAuthenticationService.ForgotPassword: Starting forgot password for user: {username}");
            var result = await _jsRuntime.InvokeAsync<ForgotPasswordResult>("CognitoAuth.forgotPassword", username);

            Console.WriteLine($"AwsCognitoAuthenticationService.ForgotPassword: Forgot password result success: {result.Success}");
            if (!string.IsNullOrEmpty(result.Error))
            {
                Console.WriteLine($"AwsCognitoAuthenticationService.ForgotPassword: Error: {result.Error}");
            }

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"AwsCognitoAuthenticationService.ForgotPassword: Exception: {ex.Message}");
            return new ForgotPasswordResult
            {
                Success = false,
                Error = $"An error occurred during forgot password: {ex.Message}"
            };
        }
    }

    public async Task<ForgotPasswordResult> ConfirmForgotPassword(string username, string verificationCode, string newPassword)
    {
        try
        {
            Console.WriteLine($"AwsCognitoAuthenticationService.ConfirmForgotPassword: Starting confirm forgot password for user: {username}");
            var result = await _jsRuntime.InvokeAsync<ForgotPasswordResult>("CognitoAuth.confirmForgotPassword", username, verificationCode, newPassword);

            Console.WriteLine($"AwsCognitoAuthenticationService.ConfirmForgotPassword: Confirm forgot password result success: {result.Success}");
            if (!string.IsNullOrEmpty(result.Error))
            {
                Console.WriteLine($"AwsCognitoAuthenticationService.ConfirmForgotPassword: Error: {result.Error}");
            }

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"AwsCognitoAuthenticationService.ConfirmForgotPassword: Exception: {ex.Message}");
            return new ForgotPasswordResult
            {
                Success = false,
                Error = $"An error occurred during confirm forgot password: {ex.Message}"
            };
        }
    }

    private async Task ClearStoredCredentials()
    {
        // Clear all localStorage data except persistent user settings and saved users
        await _localStorage.ClearAllExceptPersistentSettingsAsync();
    }
}
