using Blazing.Mvvm.ComponentModel;
using clypse.core.Cloud;
using clypse.core.Cloud.Aws.S3;
using clypse.core.Compression;
using clypse.core.Cryptography;
using clypse.core.Secrets;
using clypse.core.Vault;
using clypse.portal.Application.Services.Interfaces;
using clypse.portal.Models.Aws;
using clypse.portal.Models.WebAuthn;
using CommunityToolkit.Mvvm.Input;

namespace clypse.portal.Application.ViewModels;

/// <summary>
/// ViewModel for the Test/debug page.
/// </summary>
public partial class TestViewModel : ViewModelBase
{
    private readonly IAuthenticationService authService;
    private readonly IWebAuthnService webAuthnService;
    private readonly IJsS3InvokerProvider jsS3InvokerProvider;
    private readonly AwsCognitoConfig cognitoConfig;
    private readonly AwsS3Config awsS3Config;
    private readonly INavigationService navigationService;

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
    private string? identityId;
    private KeyDerivationBenchmarkResults? keyDerivationResults;

    // WebAuthn testing state
    private string webAuthnUsername = "demo-user";
    private bool isWebAuthnProcessing;
    private string webAuthnStatus = "Ready";
    private string webAuthnLog = string.Empty;
    private WebAuthnCredentialInfo? webAuthnCredential;

    // Login form state
    private string loginUsername = string.Empty;
    private string loginPassword = string.Empty;

    // AWS credentials (runtime, not persisted)
    private string? awsAccessKeyId;
    private string? awsSecretAccessKey;
    private string? awsSessionToken;

    /// <summary>
    /// Initializes a new instance of <see cref="TestViewModel"/>.
    /// </summary>
    /// <param name="authService">The authentication service.</param>
    /// <param name="webAuthnService">The WebAuthn service.</param>
    /// <param name="jsS3InvokerProvider">The JavaScript S3 invoker provider.</param>
    /// <param name="cognitoConfig">The AWS Cognito configuration.</param>
    /// <param name="awsS3Config">The AWS S3 configuration.</param>
    /// <param name="navigationService">The navigation service.</param>
    public TestViewModel(
        IAuthenticationService authService,
        IWebAuthnService webAuthnService,
        IJsS3InvokerProvider jsS3InvokerProvider,
        AwsCognitoConfig cognitoConfig,
        AwsS3Config awsS3Config,
        INavigationService navigationService)
    {
        this.authService = authService ?? throw new ArgumentNullException(nameof(authService));
        this.webAuthnService = webAuthnService ?? throw new ArgumentNullException(nameof(webAuthnService));
        this.jsS3InvokerProvider = jsS3InvokerProvider ?? throw new ArgumentNullException(nameof(jsS3InvokerProvider));
        this.cognitoConfig = cognitoConfig ?? throw new ArgumentNullException(nameof(cognitoConfig));
        this.awsS3Config = awsS3Config ?? throw new ArgumentNullException(nameof(awsS3Config));
        this.navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
    }

    // ─── Observable properties ────────────────────────────────────────────────

    /// <summary>Gets a value indicating whether an operation is in progress.</summary>
    public bool IsLoading { get => isLoading; private set => SetProperty(ref isLoading, value); }

    /// <summary>Gets a value indicating whether the user is authenticated.</summary>
    public bool IsAuthenticated { get => isAuthenticated; private set => SetProperty(ref isAuthenticated, value); }

    /// <summary>Gets a value indicating whether Clypse is being tested.</summary>
    public bool IsTestingClypse { get => isTestingClypse; private set => SetProperty(ref isTestingClypse, value); }

    /// <summary>Gets a value indicating whether key derivation benchmarks are running.</summary>
    public bool IsTestingKeyDerivation { get => isTestingKeyDerivation; private set => SetProperty(ref isTestingKeyDerivation, value); }

    /// <summary>Gets or sets a value indicating whether key derivation results are ready.</summary>
    public bool ShowKeyDerivationResults { get => showKeyDerivationResults; set => SetProperty(ref showKeyDerivationResults, value); }

    /// <summary>Gets the current error message.</summary>
    public string? ErrorMessage { get => errorMessage; private set => SetProperty(ref errorMessage, value); }

    /// <summary>Gets the access token.</summary>
    public string? AccessToken { get => accessToken; private set => SetProperty(ref accessToken, value); }

    /// <summary>Gets the ID token.</summary>
    public string? IdToken { get => idToken; private set => SetProperty(ref idToken, value); }

    /// <summary>Gets the Clypse test result message.</summary>
    public string? ClypseTestResult { get => clypseTestResult; private set => SetProperty(ref clypseTestResult, value); }

    /// <summary>Gets the Clypse test error message.</summary>
    public string? ClypseTestError { get => clypseTestError; private set => SetProperty(ref clypseTestError, value); }

    /// <summary>Gets the key derivation benchmark results.</summary>
    public KeyDerivationBenchmarkResults? KeyDerivationResults { get => keyDerivationResults; private set => SetProperty(ref keyDerivationResults, value); }

    /// <summary>Gets or sets the username for the login form.</summary>
    public string LoginUsername { get => loginUsername; set => SetProperty(ref loginUsername, value); }

    /// <summary>Gets or sets the password for the login form.</summary>
    public string LoginPassword { get => loginPassword; set => SetProperty(ref loginPassword, value); }

    /// <summary>Gets or sets the WebAuthn test username.</summary>
    public string WebAuthnUsername { get => webAuthnUsername; set => SetProperty(ref webAuthnUsername, value); }

    /// <summary>Gets a value indicating whether a WebAuthn operation is in progress.</summary>
    public bool IsWebAuthnProcessing { get => isWebAuthnProcessing; private set => SetProperty(ref isWebAuthnProcessing, value); }

    /// <summary>Gets the current WebAuthn status message.</summary>
    public string WebAuthnStatus { get => webAuthnStatus; private set => SetProperty(ref webAuthnStatus, value); }

    /// <summary>Gets the WebAuthn activity log.</summary>
    public string WebAuthnLog { get => webAuthnLog; private set => SetProperty(ref webAuthnLog, value); }

    /// <summary>Gets the registered WebAuthn credential info.</summary>
    public WebAuthnCredentialInfo? WebAuthnCredential { get => webAuthnCredential; private set => SetProperty(ref webAuthnCredential, value); }

    // ─── Commands ─────────────────────────────────────────────────────────────

    /// <summary>Logs in with the username/password credentials.</summary>
    [RelayCommand]
    public async Task HandleLoginAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var result = await authService.Login(LoginUsername, LoginPassword);

            if (result.Success)
            {
                IsAuthenticated = true;
                AccessToken = result.AccessToken;
                IdToken = result.IdToken;
                awsAccessKeyId = result.AwsCredentials?.AccessKeyId;
                awsSecretAccessKey = result.AwsCredentials?.SecretAccessKey;
                awsSessionToken = result.AwsCredentials?.SessionToken;
                identityId = result.AwsCredentials?.IdentityId;
                LoginUsername = string.Empty;
                LoginPassword = string.Empty;
            }
            else
            {
                ErrorMessage = result.Error ?? "Login failed";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"An error occurred: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Logs out the current user.</summary>
    [RelayCommand]
    public async Task HandleLogoutAsync()
    {
        await authService.Logout();
        IsAuthenticated = false;
        AccessToken = null;
        IdToken = null;
        awsAccessKeyId = null;
        awsSecretAccessKey = null;
        awsSessionToken = null;
        identityId = null;
        ErrorMessage = null;
        ClypseTestResult = null;
        ClypseTestError = null;
        KeyDerivationResults = null;
        ShowKeyDerivationResults = false;
    }

    /// <summary>Runs the key derivation benchmark.</summary>
    [RelayCommand]
    public async Task HandleTestKeyDerivationAsync()
    {
        IsTestingKeyDerivation = true;

        try
        {
            var keyDerivationService = new KeyDerivationService(
                new RandomGeneratorService(),
                new KeyDerivationServiceOptions());
            KeyDerivationResults = await keyDerivationService.BenchmarkAllAsync(3);
            ShowKeyDerivationResults = true;
        }
        catch (Exception ex)
        {
            ClypseTestError = $"Key derivation benchmark failed: {ex.Message}";
        }
        finally
        {
            IsTestingKeyDerivation = false;
        }
    }

    /// <summary>Runs the Clypse S3 vault test.</summary>
    [RelayCommand]
    public async Task HandleTestClypseAsync()
    {
        if (awsAccessKeyId == null || awsSecretAccessKey == null)
        {
            ClypseTestError = "No AWS credentials available";
            return;
        }

        IsTestingClypse = true;
        ClypseTestResult = null;
        ClypseTestError = null;

        try
        {
            await TestClypseAsync(awsAccessKeyId, awsSecretAccessKey);
            ClypseTestResult = "Clypse Core test completed successfully! Vault created and saved to S3.";
        }
        catch (Exception ex)
        {
            ClypseTestError = $"Clypse test failed: {ex}";
        }
        finally
        {
            IsTestingClypse = false;
        }
    }

    /// <summary>Registers a new WebAuthn credential.</summary>
    [RelayCommand]
    public async Task HandleWebAuthnRegisterAsync()
    {
        if (string.IsNullOrWhiteSpace(WebAuthnUsername))
        {
            LogWebAuthn("❌ Username is required");
            return;
        }

        IsWebAuthnProcessing = true;
        WebAuthnStatus = "Registering...";

        try
        {
            LogWebAuthn($"🔄 Starting registration for user: {WebAuthnUsername}");

            var result = await webAuthnService.RegisterAsync(WebAuthnUsername, webAuthnCredential?.CredentialID);

            if (result.Success)
            {
                WebAuthnCredential = new WebAuthnCredentialInfo
                {
                    CredentialID = result.CredentialID!,
                    UserID = result.UserID!,
                    Username = result.Username!,
                    PrfEnabled = result.PrfEnabled,
                };

                LogWebAuthn("✅ Registration successful!");
                var credId = result.CredentialID ?? string.Empty;
                LogWebAuthn($"📝 Credential ID: {credId[..Math.Min(20, credId.Length)]}...");
                LogWebAuthn($"👤 User ID: {result.UserID}");
                LogWebAuthn($"🔑 PRF Extension: {(result.PrfEnabled ? "Enabled" : "Disabled")}");
                LogWebAuthn("🎉 You can now authenticate with this credential!");
                WebAuthnStatus = "Credential registered";
            }
            else
            {
                LogWebAuthn($"❌ Registration failed: {result.Error}");
                WebAuthnStatus = "Registration failed";
            }
        }
        catch (Exception ex)
        {
            LogWebAuthn($"❌ Registration error: {ex.Message}");
            WebAuthnStatus = "Registration error";
        }
        finally
        {
            IsWebAuthnProcessing = false;
        }
    }

    /// <summary>Authenticates with the registered WebAuthn credential.</summary>
    [RelayCommand]
    public async Task HandleWebAuthnAuthenticateAsync()
    {
        if (webAuthnCredential == null)
        {
            LogWebAuthn("❌ No credential registered yet. Please register first.");
            return;
        }

        IsWebAuthnProcessing = true;
        WebAuthnStatus = "Authenticating...";

        try
        {
            LogWebAuthn($"🔐 Starting authentication for: {webAuthnCredential.Username}");

            var result = await webAuthnService.AuthenticateAsync(webAuthnCredential.CredentialID);

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
                WebAuthnStatus = "Authentication successful";
            }
            else
            {
                LogWebAuthn($"❌ Authentication failed: {result.Error}");
                WebAuthnStatus = "Authentication failed";
            }
        }
        catch (Exception ex)
        {
            LogWebAuthn($"❌ Authentication error: {ex.Message}");
            WebAuthnStatus = "Authentication error";
        }
        finally
        {
            IsWebAuthnProcessing = false;
        }
    }

    /// <summary>Clears the WebAuthn log and resets credential state.</summary>
    [RelayCommand]
    public void HandleWebAuthnClear()
    {
        WebAuthnLog = string.Empty;
        WebAuthnCredential = null;
        WebAuthnStatus = "Ready";
        LogWebAuthn("Log cleared.");
    }

    private void LogWebAuthn(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        WebAuthnLog += $"[{timestamp}] {message}\n";
    }

    private async Task TestClypseAsync(string accessKey, string secretAccessKey)
    {
        var compressionService = new GZipCompressionService();
        var jsInvoker = jsS3InvokerProvider.GetInvoker();

        var jsS3Client = new JavaScriptS3Client(
            jsInvoker,
            accessKey,
            secretAccessKey,
            awsSessionToken ?? string.Empty,
            awsS3Config.Region);

        var keyDerivationService = new KeyDerivationService(
            new RandomGeneratorService(),
            KeyDerivationServiceDefaultOptions.Blazor_Argon2id());

        var awsS3E2eCloudStorageProvider = new AwsS3E2eCloudStorageProvider(
            awsS3Config.BucketName,
            jsS3Client,
            new BouncyCastleAesGcmCryptoService());

        using var vaultManager = new VaultManager(
            identityId ?? string.Empty,
            keyDerivationService,
            compressionService,
            awsS3E2eCloudStorageProvider);

        var vault = vaultManager.Create("Foobar", "This is a test vault");

        var webSecret = new WebSecret
        {
            Name = "Foobar",
            Description = "This is a test secret.",
            Password = "password123",
        };
        webSecret.UpdateTags(["apple", "orange"]);
        vault.AddSecret(webSecret);

        var passphrase = "password123";
        var keyBytes = await vaultManager.DeriveKeyFromPassphraseAsync(vault.Info.Id, passphrase);
        var base64Key = Convert.ToBase64String(keyBytes);
        await vaultManager.SaveAsync(vault, base64Key, null, CancellationToken.None);

        var loadedVault = await vaultManager.LoadAsync(vault.Info.Id, base64Key, CancellationToken.None);
        _ = await vaultManager.GetSecretAsync(loadedVault, loadedVault.Index.Entries[0].Id, base64Key, CancellationToken.None);
    }
}
