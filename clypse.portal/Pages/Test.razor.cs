using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Security;
using clypse.core.Cloud;
using clypse.core.Cloud.Aws.S3;
using clypse.core.Compression;
using clypse.core.Cryptogtaphy;
using clypse.core.Secrets;
using clypse.core.Vault;
using clypse.portal.Models;

namespace clypse.portal.Pages;

public partial class Test : ComponentBase
{
    [Inject] public IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] public AwsCognitoConfig CognitoConfig { get; set; } = default!;
    [Inject] public AwsS3Config AwsS3Config { get; set; } = default!;

    private LoginModel loginModel = new();
    private bool isLoading = false;
    private bool isAuthenticated = false;
    private bool isTestingClypse = false;
    private bool isTestingKeyDerivation = false;
    private bool showKeyDerivationResults = false;
    private string? errorMessage;
    private string? accessToken;
    private string? idToken;
    private string? clypseTestResult;
    private string? clypseTestError;
    private AwsCredentials? awsCredentials;
    private KeyDerivationBenchmarkResults? keyDerivationResults;

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

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
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
            var keyDerivationService = new KeyDerivationService();
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
            clypseTestError = $"Clypse test failed: {ex.ToString()}";
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

        var awsS3E2eCloudStorageProvider = new AwsS3E2eCloudStorageProvider(
            AwsS3Config.BucketName,
            jsS3Client,
            new BouncyCastleAesGcmCryptoService());
        var vaultManager = new VaultManager(
            awsCredentials?.IdentityId ?? string.Empty,
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
        var secret = vault.AddSecret(webSecret);

        var password = new SecureString();
        foreach (var curChar in "password123")
        {
            password.AppendChar(curChar);
        };
        var keyDerivationService = new KeyDerivationService();
        var keyBytes = await keyDerivationService.DeriveKeyFromPassphraseAsync(
            core.Enums.KeyDerivationAlgorithm.Argon2,
            password,
            vault.Info.Base64Salt);
        var base64Key = Convert.ToBase64String(keyBytes);
        await vaultManager.SaveAsync(
            vault,
            base64Key,
            CancellationToken.None);

        var loadedVault = await vaultManager.LoadAsync(
            vault.Info.Id,
            base64Key,
            CancellationToken.None);
        var loadedWebSecret = await vaultManager.GetSecretAsync(
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
}
