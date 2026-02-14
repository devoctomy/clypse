using clypse.core.Cloud;
using clypse.core.Cloud.Aws.S3;
using clypse.core.Compression;
using clypse.core.Cryptography;
using clypse.core.Secrets;
using clypse.core.Vault;
using clypse.portal.Models.Aws;
using clypse.portal.Models.WebAuthn;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace clypse.portal.Pages;

public partial class Test : ComponentBase
{
    [Inject] public IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] public AwsCognitoConfig CognitoConfig { get; set; } = default!;
    [Inject] public AwsS3Config AwsS3Config { get; set; } = default!;
    [Inject] public NavigationManager Navigation { get; set; } = default!;

    private readonly LoginModel loginModel = new();
    private bool isLoading;
    private bool isAuthenticated;
    private bool isTestingClypse;
    private bool isTestingKeyDerivation;
    private bool showKeyDerivationResults;
    private string? errorMessage;
    private string? accessToken;
    private string? idToken;
    private string? clypseTestResult;
    private string? clypseTestError;
    private AwsCredentials? awsCredentials;
    private KeyDerivationBenchmarkResults? keyDerivationResults;
    
    // WebAuthn testing fields
    private string webAuthnUsername = "demo-user";
    private bool isWebAuthnProcessing;
    private string webAuthnCurrentAction = "";
    private string webAuthnStatus = "Ready";
    private string webAuthnLog = "";
    private WebAuthnCredentialInfo? webAuthnCredential;
    

    private class LoginModel
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    private class AwsCredentials
    {
        public string AccessKeyId { get; set; } = string.Empty;
        public string SecretAccessKey { get; set; } = string.Empty;
        public string SessionToken { get; set; } = string.Empty;
        public string Expiration { get; set; } = string.Empty;
        public string IdentityId { get; set; } = string.Empty;
    }

#pragma warning disable CS0162 // Unreachable code detected
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // Allow this for now
        ////if (!Globals.IsDebugBuild)
        ////{
        ////    Navigation.NavigateTo("/");
        ////    return;
        ////}

        if (firstRender)
        {
            // Initialize Cognito with configuration
            await JSRuntime.InvokeVoidAsync("eval", $@"
                window.cognitoConfig = {{
                    userPoolId: '{CognitoConfig.UserPoolId}',
                    userPoolClientId: '{CognitoConfig.UserPoolClientId}',
                    region: '{CognitoConfig.Region}',
                    identityPoolId: '{CognitoConfig.IdentityPoolId}'
                }};
            ");

            await JSRuntime.InvokeAsync<string>("CognitoAuth.initialize", CognitoConfig);
        }
    }
#pragma warning restore CS0162 // Unreachable code detected

    private async Task HandleLogin()
    {
        isLoading = true;
        errorMessage = null;
        StateHasChanged();

        try
        {
            var result = await JSRuntime.InvokeAsync<LoginResult>("CognitoAuth.login", loginModel.Username, loginModel.Password);

            if (result.Success)
            {
                isAuthenticated = true;
                accessToken = result.AccessToken;
                idToken = result.IdToken;

                if (result.AwsCredentials != null)
                {
                    awsCredentials = new AwsCredentials
                    {
                        AccessKeyId = result.AwsCredentials.AccessKeyId,
                        SecretAccessKey = result.AwsCredentials.SecretAccessKey,
                        SessionToken = result.AwsCredentials.SessionToken,
                        Expiration = result.AwsCredentials.Expiration,
                        IdentityId = result.AwsCredentials.IdentityId
                    };
                }

                loginModel.Username = string.Empty;
                loginModel.Password = string.Empty;
            }
            else
            {
                errorMessage = result.Error ?? "Login failed";
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"An error occurred: {ex.Message}";
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    private async Task HandleLogout()
    {
        await JSRuntime.InvokeAsync<string>("CognitoAuth.logout");
        isAuthenticated = false;
        accessToken = null;
        idToken = null;
        awsCredentials = null;
        errorMessage = null;
        clypseTestResult = null;
        clypseTestError = null;
        keyDerivationResults = null;
        showKeyDerivationResults = false;
        StateHasChanged();
    }

    private async Task HandleTestKeyDerivation()
    {
        isTestingKeyDerivation = true;
        StateHasChanged();

        try
        {
            var keyDerivationService = new KeyDerivationService(
                new RandomGeneratorService(),
                new KeyDerivationServiceOptions());
            keyDerivationResults = await keyDerivationService.BenchmarkAllAsync(3);
            showKeyDerivationResults = true;
        }
        catch (Exception ex)
        {
            clypseTestError = $"Key derivation benchmark failed: {ex.Message}";
        }
        finally
        {
            isTestingKeyDerivation = false;
            StateHasChanged();
        }
    }

    private async Task HandleTestClypse()
    {
        if (awsCredentials == null)
        {
            clypseTestError = "No AWS credentials available";
            return;
        }

        isTestingClypse = true;
        clypseTestResult = null;
        clypseTestError = null;
        StateHasChanged();

        try
        {
            await TestClypse(awsCredentials.AccessKeyId, awsCredentials.SecretAccessKey);
            clypseTestResult = "Clypse Core test completed successfully! Vault created and saved to S3.";
        }
        catch (Exception ex)
        {
            clypseTestError = $"Clypse test failed: {ex}";
        }
        finally
        {
            isTestingClypse = false;
            StateHasChanged();
        }
    }

    private async Task TestClypse(
        string accessKey,
        string secretAccessKey)
    {
        var compressionService = new GZipCompressionService();

        // Use JavaScript S3 client instead of native AWS SDK
        var jsS3Client = new JavaScriptS3Client(
            new JavaScriptS3Invoker(JSRuntime),
            accessKey,
            secretAccessKey,
            awsCredentials?.SessionToken ?? string.Empty,
            AwsS3Config.Region);

        var keyDerivationService = new KeyDerivationService(
            new RandomGeneratorService(),
            KeyDerivationServiceDefaultOptions.Blazor_Argon2id());
        var awsS3E2eCloudStorageProvider = new AwsS3E2eCloudStorageProvider(
            AwsS3Config.BucketName,
            jsS3Client,
            new BouncyCastleAesGcmCryptoService());
        using var vaultManager = new VaultManager(
            awsCredentials?.IdentityId ?? string.Empty,
            keyDerivationService,
            compressionService,
            awsS3E2eCloudStorageProvider);

        var vault = vaultManager.Create(
            "Foobar",
            "This is a test vault"
        );

        var webSecret = new WebSecret
            {
                Name = "Foobar",
                Description = "This is a test secret.",
                Password = "password123",
            };
        webSecret.UpdateTags(["apple", "orange"]);
        vault.AddSecret(webSecret);

        var passphrase = "password123";
        var keyBytes = await vaultManager.DeriveKeyFromPassphraseAsync(
            vault.Info.Id,
            passphrase);
        var base64Key = Convert.ToBase64String(keyBytes);
        await vaultManager.SaveAsync(
            vault,
            base64Key,
            null,
            CancellationToken.None);

        var loadedVault = await vaultManager.LoadAsync(
            vault.Info.Id,
            base64Key,
            CancellationToken.None);
        _ = await vaultManager.GetSecretAsync(
            loadedVault,
            loadedVault.Index.Entries[0].Id,
            base64Key,
            CancellationToken.None);
    }

    private class LoginResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public string? AccessToken { get; set; }
        public string? IdToken { get; set; }
        public JsAwsCredentials? AwsCredentials { get; set; }
    }

    private class JsAwsCredentials
    {
        public string AccessKeyId { get; set; } = string.Empty;
        public string SecretAccessKey { get; set; } = string.Empty;
        public string SessionToken { get; set; } = string.Empty;
        public string Expiration { get; set; } = string.Empty;
        public string IdentityId { get; set; } = string.Empty;
    }

    private class WebAuthnCredentialInfo
    {
        public string CredentialID { get; set; } = string.Empty;
        public string UserID { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public bool PrfEnabled { get; set; }
    }

    private void LogWebAuthn(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        webAuthnLog += $"[{timestamp}] {message}\n";
        StateHasChanged();
    }

    private async Task HandleWebAuthnRegister()
    {
        if (string.IsNullOrWhiteSpace(webAuthnUsername))
        {
            LogWebAuthn("❌ Username is required");
            return;
        }

        isWebAuthnProcessing = true;
        webAuthnCurrentAction = "register";
        webAuthnStatus = "Registering...";
        StateHasChanged();

        try
        {
            LogWebAuthn($"🔄 Starting registration for user: {webAuthnUsername}");

            var result = await JSRuntime.InvokeAsync<WebAuthnRegisterResult>("webAuthnWrapper.register", 
                webAuthnUsername, webAuthnCredential?.CredentialID);

            if (result.Success)
            {
                webAuthnCredential = new WebAuthnCredentialInfo
                {
                    CredentialID = result.CredentialID!,
                    UserID = result.UserID!,
                    Username = result.Username!,
                    PrfEnabled = result.PrfEnabled
                };

                LogWebAuthn("✅ Registration successful!");
                var credId = result.CredentialID ?? "";
                LogWebAuthn($"📝 Credential ID: {credId[..Math.Min(20, credId.Length)]}...");
                LogWebAuthn($"👤 User ID: {result.UserID}");
                LogWebAuthn($"🔑 PRF Extension: {(result.PrfEnabled ? "Enabled" : "Disabled")}");
                LogWebAuthn("🎉 You can now authenticate with this credential!");

                webAuthnStatus = "Credential registered";
            }
            else
            {
                LogWebAuthn($"❌ Registration failed: {result.Error}");
                webAuthnStatus = "Registration failed";
            }
        }
        catch (Exception ex)
        {
            LogWebAuthn($"❌ Registration error: {ex.Message}");
            webAuthnStatus = "Registration error";
        }
        finally
        {
            isWebAuthnProcessing = false;
            webAuthnCurrentAction = "";
            StateHasChanged();
        }
    }

    private async Task HandleWebAuthnAuthenticate()
    {
        if (webAuthnCredential == null)
        {
            LogWebAuthn("❌ No credential registered yet. Please register first.");
            return;
        }

        isWebAuthnProcessing = true;
        webAuthnCurrentAction = "authenticate";
        webAuthnStatus = "Authenticating...";
        StateHasChanged();

        try
        {
            LogWebAuthn($"🔐 Starting authentication for: {webAuthnCredential.Username}");

            var result = await JSRuntime.InvokeAsync<WebAuthnAuthenticateResult>("webAuthnWrapper.authenticate", 
                webAuthnCredential.CredentialID);

            if (result.Success)
            {
                LogWebAuthn("✅ Authentication successful!");
                LogWebAuthn($"👤 User Present (UP): {(result.UserPresent ? "Yes" : "No")}");
                LogWebAuthn($"🔒 User Verified (UV): {(result.UserVerified ? "Yes" : "No")}");
                
                if (!string.IsNullOrEmpty(result.PrfOutput))
                {
                    LogWebAuthn($"🔑 PRF Output: {result.PrfOutput[..Math.Min(32, result.PrfOutput.Length)]}...");
                }

                LogWebAuthn("🎊 Authentication completed successfully!");
                webAuthnStatus = "Authentication successful";
            }
            else
            {
                LogWebAuthn($"❌ Authentication failed: {result.Error}");
                webAuthnStatus = "Authentication failed";
            }
        }
        catch (Exception ex)
        {
            LogWebAuthn($"❌ Authentication error: {ex.Message}");
            webAuthnStatus = "Authentication error";
        }
        finally
        {
            isWebAuthnProcessing = false;
            webAuthnCurrentAction = "";
            StateHasChanged();
        }
    }

    private void HandleWebAuthnClear()
    {
        webAuthnLog = "";
        webAuthnCredential = null;
        webAuthnStatus = "Ready";
        LogWebAuthn("Log cleared.");
        StateHasChanged();
    }
}
