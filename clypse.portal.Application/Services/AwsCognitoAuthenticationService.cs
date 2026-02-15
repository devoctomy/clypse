using System.Text.Json;
using clypse.portal.Application.Services.Interfaces;
using clypse.portal.Models.Aws;
using clypse.portal.Models.Login;
using Microsoft.JSInterop;

namespace clypse.portal.Application.Services;

/// <inheritdoc/>
public class AwsCognitoAuthenticationService(
    IJSRuntime jsRuntime,
    AwsCognitoConfig cognitoConfig,
    ILocalStorageService localStorage)
    : IAuthenticationService
{
    private readonly IJSRuntime jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
    private readonly AwsCognitoConfig cognitoConfig = cognitoConfig ?? throw new ArgumentNullException(nameof(cognitoConfig));
    private readonly ILocalStorageService localStorage = localStorage ?? throw new ArgumentNullException(nameof(localStorage));
    private bool isInitialized;
    private bool justLoggedIn;

    /// <inheritdoc/>
    public async Task Initialize()
    {
        if (this.isInitialized)
        {
            Console.WriteLine("AwsCognitoAuthenticationService.Initialize: Already initialized, skipping");
            return;
        }

        Console.WriteLine("AwsCognitoAuthenticationService.Initialize: Starting initialization");

        // Initialize Cognito configuration in JavaScript
        var script = $@"window.cognitoConfig = {{
            userPoolId: '{this.cognitoConfig.UserPoolId}',
            userPoolClientId: '{this.cognitoConfig.UserPoolClientId}',
            region: '{this.cognitoConfig.Region}',
            identityPoolId: '{this.cognitoConfig.IdentityPoolId}'
        }};";
        await this.jsRuntime.InvokeVoidAsync(
            "eval",
            script);

        _ = await this.jsRuntime.InvokeAsync<string>("CognitoAuth.initialize", this.cognitoConfig);
        this.isInitialized = true;
        Console.WriteLine("AwsCognitoAuthenticationService.Initialize: Initialization complete");
    }

    /// <inheritdoc/>
    public async Task<bool> CheckAuthentication()
    {
        var credentials = await this.GetStoredCredentials();
        if (credentials == null)
        {
            return false;
        }

        // If we just logged in, skip the refresh to avoid double AWS API calls
        if (this.justLoggedIn)
        {
            Console.WriteLine("AwsCognitoAuthenticationService.CheckAuthentication: Just logged in, skipping credential refresh");
            this.justLoggedIn = false; // Reset the flag
            return true;
        }

        // Check if credentials are expired
        if (DateTime.TryParse(credentials.ExpirationTime, out var expirationTime))
        {
            if (expirationTime <= DateTime.UtcNow)
            {
                await this.ClearStoredCredentials();
                return false;
            }
        }

        // Verify the credentials are still valid with Cognito
        if (!string.IsNullOrEmpty(credentials.IdToken))
        {
            try
            {
                Console.WriteLine("AwsCognitoAuthenticationService.CheckAuthentication: Refreshing AWS credentials");
                var freshAwsCredentials = await this.jsRuntime.InvokeAsync<AwsCredentials>("CognitoAuth.getAwsCredentials", credentials.IdToken);
                if (freshAwsCredentials != null)
                {
                    freshAwsCredentials.IdentityId = credentials.AwsCredentials?.IdentityId ?? string.Empty;
                    credentials.AwsCredentials = freshAwsCredentials;
                    var credentialsJson = JsonSerializer.Serialize(credentials);
                    await this.jsRuntime.InvokeVoidAsync("localStorage.setItem", "clypse_credentials", credentialsJson);
                }

                return true;
            }
            catch
            {
                await this.ClearStoredCredentials();
                return false;
            }
        }

        return false;
    }

    /// <inheritdoc/>
    public async Task<LoginResult> Login(string username, string password)
    {
        try
        {
            Console.WriteLine($"AwsCognitoAuthenticationService.Login: Starting login for user: {username}");
            var result = await this.jsRuntime.InvokeAsync<LoginResult>("CognitoAuth.login", username, password);

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
                await this.StoreCredentials(result);
                this.justLoggedIn = true; // Set flag to avoid double credential refresh
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
                Error = $"An error occurred during login: {ex.Message}",
            };
        }
    }

    /// <inheritdoc/>
    public async Task Logout()
    {
        await this.jsRuntime.InvokeVoidAsync("CognitoAuth.logout");
        await this.ClearStoredCredentials();
    }

    /// <inheritdoc/>
    public async Task<StoredCredentials?> GetStoredCredentials()
    {
        try
        {
            var credentialsJson = await this.jsRuntime.InvokeAsync<string>("localStorage.getItem", "clypse_credentials");

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

    /// <inheritdoc/>
    public async Task<LoginResult> CompletePasswordReset(string username, string newPassword)
    {
        try
        {
            Console.WriteLine($"AwsCognitoAuthenticationService.CompletePasswordReset: Starting password reset for user: {username}");
            var result = await this.jsRuntime.InvokeAsync<LoginResult>("CognitoAuth.completePasswordReset", username, newPassword);

            Console.WriteLine($"AwsCognitoAuthenticationService.CompletePasswordReset: Password reset result success: {result.Success}");
            if (!string.IsNullOrEmpty(result.Error))
            {
                Console.WriteLine($"AwsCognitoAuthenticationService.CompletePasswordReset: Error: {result.Error}");
            }

            if (result.Success)
            {
                Console.WriteLine("AwsCognitoAuthenticationService.CompletePasswordReset: Storing credentials");
                await this.StoreCredentials(result);
                this.justLoggedIn = true;
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
                Error = $"An error occurred during password reset: {ex.Message}",
            };
        }
    }

    /// <inheritdoc/>
    public async Task<ForgotPasswordResult> ForgotPassword(string username)
    {
        try
        {
            Console.WriteLine($"AwsCognitoAuthenticationService.ForgotPassword: Starting forgot password for user: {username}");
            var result = await this.jsRuntime.InvokeAsync<ForgotPasswordResult>("CognitoAuth.forgotPassword", username);

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
                Error = $"An error occurred during forgot password: {ex.Message}",
            };
        }
    }

    /// <inheritdoc/>
    public async Task<ForgotPasswordResult> ConfirmForgotPassword(string username, string verificationCode, string newPassword)
    {
        try
        {
            Console.WriteLine($"AwsCognitoAuthenticationService.ConfirmForgotPassword: Starting confirm forgot password for user: {username}");
            var result = await this.jsRuntime.InvokeAsync<ForgotPasswordResult>("CognitoAuth.confirmForgotPassword", username, verificationCode, newPassword);

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
                Error = $"An error occurred during confirm forgot password: {ex.Message}",
            };
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
            StoredAt = DateTime.UtcNow,
        };

        if (result.AwsCredentials != null)
        {
            Console.WriteLine($"AwsCognitoAuthenticationService.StoreCredentials: Storing AWS Credentials with IdentityId: '{result.AwsCredentials.IdentityId}'");
        }

        var credentialsJson = JsonSerializer.Serialize(credentials);
        Console.WriteLine($"AwsCognitoAuthenticationService.StoreCredentials: Serialized credentials length: {credentialsJson.Length}");

        await this.jsRuntime.InvokeVoidAsync("localStorage.setItem", "clypse_credentials", credentialsJson);
        Console.WriteLine("AwsCognitoAuthenticationService.StoreCredentials: Credentials stored successfully");
    }

    private async Task ClearStoredCredentials()
    {
        // Clear all localStorage data except persistent user settings and saved users
        await this.localStorage.ClearAllExceptPersistentSettingsAsync();
    }
}
