using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
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
    [Inject] public NavigationManager Navigation { get; set; } = default!;

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
    
    // WebAuthn test result display properties
    private string? webAuthnTestMessage;
    private bool webAuthnTestSuccess;
    
    // SimpleWebAuthn test result display properties
    private string? simpleWebAuthnTestMessage;
    private bool simpleWebAuthnTestSuccess;
    private string? storedCredentialId;

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

    private void ClearWebAuthnMessage()
    {
        webAuthnTestMessage = null;
        webAuthnTestSuccess = false;
    }

    private void SetWebAuthnMessage(string message, bool success)
    {
        webAuthnTestMessage = message;
        webAuthnTestSuccess = success;
    }

    private async Task HandleWebAuthnEncrypt()
    {
        ClearWebAuthnMessage();
        
        try
        {
            Console.WriteLine("Starting WebAuthn encryption...");
            
            // Use WebAuthn to authenticate and encrypt text
            var result = await JSRuntime.InvokeAsync<WebAuthnResult>("WebAuthnPrf.encrypt", "hello world");
            
            Console.WriteLine($"WebAuthn result received - Success: {result?.Success}, Error: {result?.Error}");
            
            if (result?.Success == true)
            {
                // Store the encrypted data in localStorage
                await JSRuntime.InvokeVoidAsync("localStorage.setItem", "webauthn_encrypted_data", result.EncryptedDataBase64);
                
                // Log the base64 encoded cipher text
                Console.WriteLine($"WebAuthn Encrypted Data (Base64): {result.EncryptedDataBase64}");
                Console.WriteLine($"Key Derivation Method: {result.KeyDerivationMethod ?? "Unknown"}");
                
                // Build detailed diagnostic message for mobile viewing
                var method = result.KeyDerivationMethod == "PRF" ? "PRF (biometric)" : "Credential ID (PIN)";
                var diagnostics = result.Diagnostics;
                
                var message = $"<strong>✅ Encryption successful using {method} method!</strong><br><br>" +
                             $"🔧 <strong>Platform:</strong> {diagnostics?.Platform}<br>" +
                             $"🔑 <strong>Method:</strong> {result.KeyDerivationMethod}<br>" +
                             $"🛡️ <strong>Authenticator:</strong> {diagnostics?.AuthenticatorType}<br>" +
                             $"📊 <strong>PRF Supported:</strong> {(diagnostics?.PrfSupported == true ? "Yes" : "No")}<br>" +
                             $"📋 <strong>PRF Results:</strong> {(diagnostics?.PrfResultsAvailable == true ? "Available" : "Not Available")}<br>" +
                             $"🆔 <strong>Credential ID:</strong> {diagnostics?.CredentialIdLength} bytes<br>" +
                             $"💾 <strong>Data stored in localStorage</strong>";
                             
                SetWebAuthnMessage(message, true);
                
                // Clear old test results
                clypseTestResult = null;
                clypseTestError = null;
            }
            else
            {
                SetWebAuthnMessage(result?.Error ?? "Unknown encryption error", false);
                clypseTestResult = null;
                clypseTestError = null;
            }
        }
        catch (Exception ex)
        {
            SetWebAuthnMessage($"Exception during encryption: {ex.Message}", false);
            clypseTestResult = null;
            clypseTestError = null;
        }
        
        StateHasChanged();
    }

    private async Task HandleWebAuthnDecrypt()
    {
        ClearWebAuthnMessage();
        
        try
        {
            // Check if encrypted data exists first
            var encryptedData = await JSRuntime.InvokeAsync<string>("localStorage.getItem", "webauthn_encrypted_data");
            
            if (string.IsNullOrEmpty(encryptedData))
            {
                // No encrypted data found - do nothing (no error message)
                Console.WriteLine("No encrypted data found in localStorage - skipping decrypt test");
                return;
            }
            
            Console.WriteLine("Starting WebAuthn decryption test...");
            
            // Use WebAuthn to authenticate and decrypt stored data
            var result = await JSRuntime.InvokeAsync<WebAuthnDecryptResult>("WebAuthnPrf.decrypt", "");
            
            Console.WriteLine($"WebAuthn decrypt result received - Success: {result?.Success}, Error: {result?.Error}");
            
            if (result?.Success == true)
            {
                Console.WriteLine($"Decrypted plaintext: '{result.Plaintext}'");
                Console.WriteLine($"Key Derivation Method: {result.KeyDerivationMethod ?? "Unknown"}");
                
                // Check if decrypted text matches expected value
                if (result.Plaintext == "hello world")
                {
                    var method = result.KeyDerivationMethod == "PRF" ? "PRF (biometric)" : "Credential ID (PIN)";
                    var diagnostics = result.Diagnostics;
                    
                    var message = $"<strong>✅ SUCCESSFUL TEST! Decryption successful!</strong><br><br>" +
                                 $"📝 <strong>Decrypted:</strong> '{result.Plaintext}'<br>" +
                                 $"🔧 <strong>Platform:</strong> {diagnostics?.Platform}<br>" +
                                 $"🔑 <strong>Method:</strong> {result.KeyDerivationMethod}<br>" +
                                 $"🛡️ <strong>Authenticator:</strong> {diagnostics?.AuthenticatorType}<br>" +
                                 $"📊 <strong>PRF Supported:</strong> {(diagnostics?.PrfSupported == true ? "Yes" : "No")}<br>" +
                                 $"📋 <strong>PRF Results:</strong> {(diagnostics?.PrfResultsAvailable == true ? "Available" : "Not Available")}<br>" +
                                 $"🆔 <strong>Credential ID:</strong> {diagnostics?.CredentialIdLength} bytes";
                                 
                    SetWebAuthnMessage(message, true);
                    Console.WriteLine("WebAuthn decrypt test PASSED - plaintext matches expected value");
                }
                else
                {
                    var message = $"<strong>❌ Decryption completed but plaintext mismatch!</strong><br>" +
                                 $"<strong>Expected:</strong> 'hello world'<br>" +
                                 $"<strong>Got:</strong> '{result.Plaintext}'<br>" +
                                 $"<strong>Method:</strong> {result.KeyDerivationMethod}";
                    SetWebAuthnMessage(message, false);
                    Console.WriteLine($"WebAuthn decrypt test FAILED - plaintext mismatch");
                }
                
                // Clear old test results
                clypseTestResult = null;
                clypseTestError = null;
            }
            else
            {
                SetWebAuthnMessage(result?.Error ?? "Unknown decryption error", false);
                clypseTestResult = null;
                clypseTestError = null;
            }
        }
        catch (Exception ex)
        {
            SetWebAuthnMessage($"Exception during decryption: {ex.Message}", false);
            clypseTestResult = null;
            clypseTestError = null;
        }
        
        StateHasChanged();
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

    private class WebAuthnResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public string? EncryptedDataBase64 { get; set; }
        public string? KeyDerivationMethod { get; set; }
        public WebAuthnDiagnostics? Diagnostics { get; set; }
    }

    private class WebAuthnDiagnostics
    {
        public string? UserAgent { get; set; }
        public string? Platform { get; set; }
        public bool PrfSupported { get; set; }
        public bool PrfResultsAvailable { get; set; }
        public int CredentialIdLength { get; set; }
        public string? AuthenticatorType { get; set; }
    }

    private class WebAuthnDecryptResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public string? Plaintext { get; set; }
        public string? KeyDerivationMethod { get; set; }
        public WebAuthnDiagnostics? Diagnostics { get; set; }
    }

    private void ClearSimpleWebAuthnMessage()
    {
        simpleWebAuthnTestMessage = null;
        simpleWebAuthnTestSuccess = false;
    }

    private void SetSimpleWebAuthnMessage(string message, bool success)
    {
        simpleWebAuthnTestMessage = message;
        simpleWebAuthnTestSuccess = success;
    }

    private async Task HandleSimpleWebAuthnCreateCredential()
    {
        ClearSimpleWebAuthnMessage();
        
        try
        {
            Console.WriteLine("Starting SimpleWebAuthn createCredential...");
            
            // Create credential with the new library
            var result = await JSRuntime.InvokeAsync<SimpleWebAuthnResult>("SimpleWebAuthn.createCredential", new {
                rp = new { name = "Clypse Test", id = "localhost" },
                user = new { 
                    id = "dGVzdC11c2VyLWlkLTEyMzQ=", // base64 of "test-user-id-1234"
                    name = "testuser@example.com",
                    displayName = "Test User"
                },
                challenge = "Y2hhbGxlbmdlLTE2LWJ5dGVzLXRlc3Q=", // base64 of "challenge-16-bytes-test"
                pubKeyCredParams = new[] { 
                    new { alg = -7, type = "public-key" },
                    new { alg = -257, type = "public-key" }
                },
                authenticatorSelection = new {
                    authenticatorAttachment = "platform",
                    userVerification = "required",
                    residentKey = "preferred"
                },
                timeout = 60000,
                attestation = "none",
                plaintextToEncrypt = "hello world from SimpleWebAuthn!",  // Data to encrypt during creation
                encryptionSalt = "clypse-test-vault-salt-v1"  // Required salt parameter
            });
            
            Console.WriteLine($"SimpleWebAuthn createCredential result - Success: {result?.Success}, Error: {result?.Error}");
            
            if (result?.Success == true && !string.IsNullOrEmpty(result.CredentialId))
            {
                // Store the credential ID for authentication test
                storedCredentialId = result.CredentialId;
                await JSRuntime.InvokeVoidAsync("localStorage.setItem", "simple_webauthn_credential_id", result.CredentialId);
                
                // Store the encrypted data if encryption was performed
                if (!string.IsNullOrEmpty(result.EncryptedData))
                {
                    await JSRuntime.InvokeVoidAsync("localStorage.setItem", "simple_webauthn_encrypted_data", result.EncryptedData);
                }
                
                Console.WriteLine($"Credential created with ID: {result.CredentialId}");
                Console.WriteLine($"Credential ID length: {result.CredentialId?.Length}");
                Console.WriteLine($"Credential ID is valid base64: {IsValidBase64(result.CredentialId)}");
                Console.WriteLine($"Key Derivation Method: {result.KeyDerivationMethod ?? "Unknown"}");
                
                var method = result.KeyDerivationMethod == "PRF" ? "PRF (biometric)" : "Credential ID (PIN)";
                var diagnostics = result.Diagnostics;
                
                var message = $"<strong>✅ Credential created successfully using {method} method!</strong><br><br>" +
                             $"🆔 <strong>Credential ID:</strong> {result.CredentialId}<br>" +
                             $"� <strong>Encrypted Data:</strong> {result.EncryptedData}<br>" +
                             $"�🔧 <strong>Platform:</strong> {diagnostics?.Platform}<br>" +
                             $"🔑 <strong>Method:</strong> {result.KeyDerivationMethod}<br>" +
                             $"🛡️ <strong>Authenticator:</strong> {diagnostics?.AuthenticatorType}<br>" +
                             $"📊 <strong>PRF Supported:</strong> {(diagnostics?.PrfSupported == true ? "Yes" : "No")}<br>" +
                             $"📋 <strong>PRF Results:</strong> {(diagnostics?.PrfResultsAvailable == true ? "Available" : "Not Available")}<br>" +
                             $"💾 <strong>Ready for authentication test</strong>";
                             
                SetSimpleWebAuthnMessage(message, true);
            }
            else
            {
                SetSimpleWebAuthnMessage(result?.Error ?? "Unknown credential creation error", false);
            }
        }
        catch (Exception ex)
        {
            SetSimpleWebAuthnMessage($"Exception during credential creation: {ex.Message}", false);
        }
        
        StateHasChanged();
    }

    private async Task HandleSimpleWebAuthnAuthenticate()
    {
        ClearSimpleWebAuthnMessage();
        
        try
        {
            // Get the stored credential ID and encrypted data
            var credentialId = storedCredentialId ?? await JSRuntime.InvokeAsync<string>("localStorage.getItem", "simple_webauthn_credential_id");
            var encryptedData = await JSRuntime.InvokeAsync<string>("localStorage.getItem", "simple_webauthn_encrypted_data");
            
            if (string.IsNullOrEmpty(credentialId))
            {
                SetSimpleWebAuthnMessage("No credential found. Please create a credential first.", false);
                StateHasChanged();
                return;
            }
            
            Console.WriteLine("Starting SimpleWebAuthn authenticate...");
            Console.WriteLine($"Using credential ID: {credentialId}");
            Console.WriteLine($"Credential ID length: {credentialId?.Length}");
            Console.WriteLine($"Credential ID is valid base64: {IsValidBase64(credentialId)}");
            
            // Authenticate with the new library
            var result = await JSRuntime.InvokeAsync<SimpleWebAuthnAuthResult>("SimpleWebAuthn.authenticate", new {
                challenge = "YXV0aC1jaGFsbGVuZ2UtMTYtYnl0ZXM=", // base64 of "auth-challenge-16-bytes"
                allowCredentials = new[] {
                    new { 
                        id = credentialId,
                        type = "public-key",
                        transports = new[] { "internal", "hybrid" }
                    }
                },
                userVerification = "required",
                timeout = 60000,
                // Encrypted data to decrypt (if available)
                encryptedData = encryptedData,
                encryptionSalt = "clypse-test-vault-salt-v1"  // Required salt parameter - must match createCredential
            });
            
            Console.WriteLine($"SimpleWebAuthn authenticate result - Success: {result?.Success}, Error: {result?.Error}");
            
            if (result?.Success == true)
            {
                Console.WriteLine($"Authentication successful with key: {result.DerivedKey?.Substring(0, 16)}...");
                Console.WriteLine($"Decrypted data: {result.DecryptedData}");
                Console.WriteLine($"Key Derivation Method: {result.KeyDerivationMethod ?? "Unknown"}");
                
                var method = result.KeyDerivationMethod == "PRF" ? "PRF (biometric)" : "Credential ID (PIN)";
                var diagnostics = result.Diagnostics;
                
                var message = $"<strong>✅ AUTHENTICATION SUCCESSFUL!</strong><br><br>" +
                             $"🔑 <strong>Method:</strong> {result.KeyDerivationMethod}<br>" +
                             $"🗝️ <strong>Derived Key:</strong> {result.DerivedKey?.Substring(0, 32)}...<br>" +
                             $"� <strong>Decrypted Data:</strong> {result.DecryptedData}<br>" +
                             $"🔧 <strong>Platform:</strong> {diagnostics?.Platform}<br>" +
                             $"🛡️ <strong>Authenticator:</strong> {diagnostics?.AuthenticatorType}<br>" +
                             $"📊 <strong>PRF Supported:</strong> {(diagnostics?.PrfSupported == true ? "Yes" : "No")}<br>" +
                             $"📋 <strong>PRF Results:</strong> {(diagnostics?.PrfResultsAvailable == true ? "Available" : "Not Available")}<br>" +
                             $"🆔 <strong>Credential ID:</strong> {result.CredentialId}";
                             
                SetSimpleWebAuthnMessage(message, true);
            }
            else
            {
                SetSimpleWebAuthnMessage(result?.Error ?? "Unknown authentication error", false);
            }
        }
        catch (Exception ex)
        {
            SetSimpleWebAuthnMessage($"Exception during authentication: {ex.Message}", false);
        }
        
        StateHasChanged();
    }

    // SimpleWebAuthn result classes
    private class SimpleWebAuthnResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public string? CredentialId { get; set; }
        public string? EncryptedData { get; set; }
        public string? KeyDerivationMethod { get; set; }
        public WebAuthnDiagnostics? Diagnostics { get; set; }
    }

    private class SimpleWebAuthnAuthResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public string? CredentialId { get; set; }
        public string? DerivedKey { get; set; }
        public string? DecryptedData { get; set; }
        public string? KeyDerivationMethod { get; set; }
        public WebAuthnDiagnostics? Diagnostics { get; set; }
    }

    private static bool IsValidBase64(string? value)
    {
        if (string.IsNullOrEmpty(value)) return false;
        try
        {
            Convert.FromBase64String(value);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
