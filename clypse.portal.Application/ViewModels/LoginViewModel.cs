using System.Text.Json;
using Blazing.Mvvm.ComponentModel;
using clypse.core.Cryptography.Interfaces;
using clypse.portal.Application.Services.Interfaces;
using clypse.portal.Models.Login;
using clypse.portal.Models.Settings;
using CommunityToolkit.Mvvm.Input;

namespace clypse.portal.Application.ViewModels;

/// <summary>
/// ViewModel for the login page, managing authentication flows including standard login,
/// WebAuthn, forgot password, and saved users.
/// </summary>
public partial class LoginViewModel : ViewModelBase
{
    private const string SavedUsersStorageKey = "users";

    private readonly IAuthenticationService authService;
    private readonly INavigationService navigationService;
    private readonly IUserSettingsService userSettingsService;
    private readonly IBrowserInteropService browserInteropService;
    private readonly ILocalStorageService localStorageService;
    private readonly IWebAuthnService webAuthnService;
    private readonly ICryptoService cryptoService;
    private readonly AppSettings appSettings;

    private bool isLoading;
    private string? errorMessage;
    private string currentTheme = "light";
    private string themeIcon = "bi-moon";
    private List<SavedUser> savedUsers = [];
    private bool showUsersList;
    private bool showRememberMe = true;
    private bool rememberMe;
    private bool isUsernameReadonly;
    private bool passwordResetRequired;
    private bool rememberMeWhenResetStarted;
    private string newPassword = string.Empty;
    private string confirmPassword = string.Empty;
    private bool forgotPasswordMode;
    private bool forgotPasswordCodeSent;
    private string forgotPasswordUsername = string.Empty;
    private string verificationCode = string.Empty;
    private bool showWebAuthnPrompt;
    private bool isWebAuthnProcessing;
    private string? webAuthnErrorMessage;
    private string username = string.Empty;
    private string password = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoginViewModel"/> class.
    /// </summary>
    /// <param name="authService">The authentication service.</param>
    /// <param name="navigationService">The navigation service.</param>
    /// <param name="userSettingsService">The user settings service.</param>
    /// <param name="browserInteropService">The browser interop service.</param>
    /// <param name="localStorageService">The local storage service.</param>
    /// <param name="webAuthnService">The WebAuthn service for passkey authentication.</param>
    /// <param name="cryptoService">The cryptographic service.</param>
    /// <param name="appSettings">The application settings.</param>
    public LoginViewModel(
        IAuthenticationService authService,
        INavigationService navigationService,
        IUserSettingsService userSettingsService,
        IBrowserInteropService browserInteropService,
        ILocalStorageService localStorageService,
        IWebAuthnService webAuthnService,
        ICryptoService cryptoService,
        AppSettings appSettings)
    {
        this.authService = authService ?? throw new ArgumentNullException(nameof(authService));
        this.navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        this.userSettingsService = userSettingsService ?? throw new ArgumentNullException(nameof(userSettingsService));
        this.browserInteropService = browserInteropService ?? throw new ArgumentNullException(nameof(browserInteropService));
        this.localStorageService = localStorageService ?? throw new ArgumentNullException(nameof(localStorageService));
        this.webAuthnService = webAuthnService ?? throw new ArgumentNullException(nameof(webAuthnService));
        this.cryptoService = cryptoService ?? throw new ArgumentNullException(nameof(cryptoService));
        this.appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
    }

    /// <summary>Gets the application settings.</summary>
    public AppSettings AppSettings => appSettings;

    /// <summary>Gets or sets a value indicating whether an operation is in progress.</summary>
    public bool IsLoading { get => isLoading; set => SetProperty(ref isLoading, value); }

    /// <summary>Gets or sets the error message to display.</summary>
    public string? ErrorMessage { get => errorMessage; set => SetProperty(ref errorMessage, value); }

    /// <summary>Gets or sets the current theme name.</summary>
    public string CurrentTheme { get => currentTheme; set => SetProperty(ref currentTheme, value); }

    /// <summary>Gets or sets the theme icon class.</summary>
    public string ThemeIcon { get => themeIcon; set => SetProperty(ref themeIcon, value); }

    /// <summary>Gets the list of saved users.</summary>
    public List<SavedUser> SavedUsers { get => savedUsers; private set => SetProperty(ref savedUsers, value); }

    /// <summary>Gets or sets a value indicating whether the saved users list is shown.</summary>
    public bool ShowUsersList { get => showUsersList; set => SetProperty(ref showUsersList, value); }

    /// <summary>Gets or sets a value indicating whether the remember me checkbox is shown.</summary>
    public bool ShowRememberMe { get => showRememberMe; set => SetProperty(ref showRememberMe, value); }

    /// <summary>Gets or sets a value indicating whether the user wants to be remembered.</summary>
    public bool RememberMe { get => rememberMe; set => SetProperty(ref rememberMe, value); }

    /// <summary>Gets or sets a value indicating whether the username field is readonly.</summary>
    public bool IsUsernameReadonly { get => isUsernameReadonly; set => SetProperty(ref isUsernameReadonly, value); }

    /// <summary>Gets or sets a value indicating whether a password reset is required.</summary>
    public bool PasswordResetRequired { get => passwordResetRequired; set => SetProperty(ref passwordResetRequired, value); }

    /// <summary>Gets or sets the new password for reset.</summary>
    public string NewPassword { get => newPassword; set => SetProperty(ref newPassword, value); }

    /// <summary>Gets or sets the confirm password for reset.</summary>
    public string ConfirmPassword { get => confirmPassword; set => SetProperty(ref confirmPassword, value); }

    /// <summary>Gets or sets a value indicating whether the forgot password mode is active.</summary>
    public bool ForgotPasswordMode { get => forgotPasswordMode; set => SetProperty(ref forgotPasswordMode, value); }

    /// <summary>Gets or sets a value indicating whether the forgot password code has been sent.</summary>
    public bool ForgotPasswordCodeSent { get => forgotPasswordCodeSent; set => SetProperty(ref forgotPasswordCodeSent, value); }

    /// <summary>Gets or sets the username for forgot password flow.</summary>
    public string ForgotPasswordUsername { get => forgotPasswordUsername; set => SetProperty(ref forgotPasswordUsername, value); }

    /// <summary>Gets or sets the verification code for password reset.</summary>
    public string VerificationCode { get => verificationCode; set => SetProperty(ref verificationCode, value); }

    /// <summary>Gets or sets a value indicating whether the WebAuthn setup prompt is shown.</summary>
    public bool ShowWebAuthnPrompt { get => showWebAuthnPrompt; set => SetProperty(ref showWebAuthnPrompt, value); }

    /// <summary>Gets or sets a value indicating whether WebAuthn processing is in progress.</summary>
    public bool IsWebAuthnProcessing { get => isWebAuthnProcessing; set => SetProperty(ref isWebAuthnProcessing, value); }

    /// <summary>Gets or sets the WebAuthn error message.</summary>
    public string? WebAuthnErrorMessage { get => webAuthnErrorMessage; set => SetProperty(ref webAuthnErrorMessage, value); }

    /// <summary>Gets or sets the username field value.</summary>
    public string Username { get => username; set => SetProperty(ref username, value); }

    /// <summary>Gets or sets the password field value.</summary>
    public string Password { get => password; set => SetProperty(ref password, value); }

    /// <inheritdoc/>
    public override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await authService.Initialize();
            await InitializeThemeAsync();
            await LoadSavedUsersAsync();

            if (await authService.CheckAuthentication())
            {
                navigationService.NavigateTo("/");
            }
        }
    }

    /// <summary>Toggles between light and dark themes.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RelayCommand]
    public async Task ToggleThemeAsync()
    {
        CurrentTheme = CurrentTheme == "light" ? "dark" : "light";
        ThemeIcon = CurrentTheme == "light" ? "bi-moon" : "bi-sun";
        await userSettingsService.SetThemeAsync(CurrentTheme);
        await browserInteropService.SetThemeAsync(CurrentTheme);
    }

    /// <summary>Selects a saved user for login.</summary>
    /// <param name="user">The saved user to select.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RelayCommand]
    public async Task SelectUserAsync(SavedUser user)
    {
        if (user.WebAuthnCredential != null && !string.IsNullOrEmpty(user.WebAuthnCredential.EncryptedPassword))
        {
            await AttemptWebAuthnLoginAsync(user);
        }
        else
        {
            Username = user.Email;
            IsUsernameReadonly = false;
            ShowUsersList = false;
            ShowRememberMe = false;
        }
    }

    /// <summary>Removes a saved user from the list.</summary>
    /// <param name="user">The saved user to remove.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RelayCommand]
    public async Task RemoveUserAsync(SavedUser user)
    {
        try
        {
            savedUsers.Remove(user);
            SavedUsers = [.. savedUsers];

            var usersData = new SavedUsersData { Users = savedUsers };
            await localStorageService.SetItemAsync(SavedUsersStorageKey, JsonSerializer.Serialize(usersData));

            if (savedUsers.Count == 0)
            {
                ShowUsersList = false;
                ShowRememberMe = true;
            }
        }
        catch
        {
            // Continue silently
        }
    }

    /// <summary>Shows the login form instead of the saved users list.</summary>
    [RelayCommand]
    public void ShowLoginForm()
    {
        ShowUsersList = false;
        ShowRememberMe = true;
        IsUsernameReadonly = false;
        Username = string.Empty;
        Password = string.Empty;
        RememberMe = false;
    }

    /// <summary>Shows the saved users list.</summary>
    [RelayCommand]
    public void ShowUsersListCommand()
    {
        ShowUsersList = true;
        ShowRememberMe = false;
        Username = string.Empty;
        Password = string.Empty;
        RememberMe = false;
    }

    /// <summary>Handles the login form submission.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RelayCommand]
    public async Task HandleLoginAsync()
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var result = await authService.Login(Username, Password);

            if (result.Success)
            {
                var rememberMe = RememberMe && !string.IsNullOrEmpty(Username);
                await DoLogin(rememberMe);
            }
            else if (result.PasswordResetRequired)
            {
                PasswordResetRequired = true;
                rememberMeWhenResetStarted = RememberMe;
                ErrorMessage = result.Error ?? "Password reset required";
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

    /// <summary>Handles the password change (reset) form submission.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RelayCommand]
    public async Task HandlePasswordChangeAsync()
    {
        if (string.IsNullOrEmpty(NewPassword) || NewPassword != ConfirmPassword)
        {
            ErrorMessage = "Passwords do not match";
            return;
        }

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var result = await authService.CompletePasswordReset(Username, NewPassword);

            if (result.Success)
            {
                var rememberMe = rememberMeWhenResetStarted && !string.IsNullOrEmpty(Username);
                await DoLogin(rememberMe);
            }
            else
            {
                ErrorMessage = result.Error ?? "Failed to change password";
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

    /// <summary>Sets up WebAuthn biometric login after successful authentication.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RelayCommand]
    public async Task HandleWebAuthnSetupAsync()
    {
        IsWebAuthnProcessing = true;
        WebAuthnErrorMessage = null;

        try
        {
            var result = await webAuthnService.RegisterAsync(Username, null);

            if (result.Success && result.PrfEnabled)
            {
                string? encryptedPassword = null;

                if (!string.IsNullOrEmpty(result.PrfOutput))
                {
                    try
                    {
                        encryptedPassword = await EncryptPasswordWithPrfAsync(Password, result.PrfOutput);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to encrypt password: {ex.Message}");
                    }
                }

                var webAuthnCredential = new WebAuthnStoredCredential
                {
                    CredentialID = result.CredentialID ?? string.Empty,
                    UserID = result.UserID ?? string.Empty,
                    CreatedAt = DateTime.UtcNow,
                    EncryptedPassword = encryptedPassword,
                };

                await SaveUserAsync(Username, webAuthnCredential);
                navigationService.NavigateTo("/");
            }
            else if (result.Success && !result.PrfEnabled)
            {
                WebAuthnErrorMessage = "Your authenticator doesn't support the required security features for biometric login. Please continue with regular login.";
            }
            else
            {
                WebAuthnErrorMessage = result.Error ?? "Failed to set up biometric login. Please try again or continue with regular login.";
            }
        }
        catch (Exception ex)
        {
            WebAuthnErrorMessage = $"Error setting up biometric login: {ex.Message}";
        }
        finally
        {
            IsWebAuthnProcessing = false;
        }
    }

    /// <summary>Skips WebAuthn setup and navigates to the app.</summary>
    [RelayCommand]
    public void SkipWebAuthnSetup()
    {
        ShowWebAuthnPrompt = false;
        navigationService.NavigateTo("/");
    }

    /// <summary>Dismisses the WebAuthn error and navigates to the app.</summary>
    [RelayCommand]
    public void DismissWebAuthnError()
    {
        WebAuthnErrorMessage = null;
        ShowWebAuthnPrompt = false;
        navigationService.NavigateTo("/");
    }

    /// <summary>Shows the forgot password mode.</summary>
    [RelayCommand]
    public void ShowForgotPassword()
    {
        ForgotPasswordMode = true;
        ForgotPasswordCodeSent = false;
        ForgotPasswordUsername = string.Empty;
        VerificationCode = string.Empty;
        NewPassword = string.Empty;
        ConfirmPassword = string.Empty;
        ErrorMessage = null;
    }

    /// <summary>Cancels the forgot password flow.</summary>
    [RelayCommand]
    public void CancelForgotPassword()
    {
        ForgotPasswordMode = false;
        ForgotPasswordCodeSent = false;
        ForgotPasswordUsername = string.Empty;
        VerificationCode = string.Empty;
        NewPassword = string.Empty;
        ConfirmPassword = string.Empty;
        ErrorMessage = null;
    }

    /// <summary>Sends the forgot password code.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RelayCommand]
    public async Task HandleForgotPasswordAsync()
    {
        if (string.IsNullOrWhiteSpace(ForgotPasswordUsername))
        {
            ErrorMessage = "Please enter your username or email address";
            return;
        }

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var result = await authService.ForgotPassword(ForgotPasswordUsername);

            if (result.Success)
            {
                ForgotPasswordCodeSent = true;
                ErrorMessage = null;
            }
            else
            {
                ErrorMessage = result.Error ?? "Failed to send verification code";
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

    /// <summary>Confirms the forgot password with a new password.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [RelayCommand]
    public async Task HandleConfirmForgotPasswordAsync()
    {
        if (string.IsNullOrWhiteSpace(VerificationCode))
        {
            ErrorMessage = "Please enter the verification code";
            return;
        }

        if (string.IsNullOrEmpty(NewPassword))
        {
            ErrorMessage = "Please enter a new password";
            return;
        }

        if (NewPassword != ConfirmPassword)
        {
            ErrorMessage = "Passwords do not match";
            return;
        }

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var result = await authService.ConfirmForgotPassword(ForgotPasswordUsername, VerificationCode, NewPassword);

            if (result.Success)
            {
                ErrorMessage = "Password reset successfully! You can now login with your new password.";
                ForgotPasswordMode = false;
                ForgotPasswordCodeSent = false;
                Username = ForgotPasswordUsername;
                Password = string.Empty;
                ForgotPasswordUsername = string.Empty;
                VerificationCode = string.Empty;
                NewPassword = string.Empty;
                ConfirmPassword = string.Empty;
            }
            else
            {
                ErrorMessage = result.Error ?? "Failed to reset password";
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

    private async Task DoLogin(bool rememberMe)
    {
        if (rememberMe)
        {
            await SaveUserAsync(Username);

            if (appSettings.EnablePortalLoginAuthn)
            {
                ShowWebAuthnPrompt = true;
            }
            else
            {
                navigationService.NavigateTo("/");
            }
        }
        else
        {
            navigationService.NavigateTo("/");
        }
    }

    private async Task InitializeThemeAsync()
    {
        try
        {
            CurrentTheme = await userSettingsService.GetThemeAsync();
            ThemeIcon = CurrentTheme == "light" ? "bi-moon" : "bi-sun";
            await browserInteropService.SetThemeAsync(CurrentTheme);
        }
        catch
        {
            CurrentTheme = "light";
            ThemeIcon = "bi-moon";
        }
    }

    private async Task LoadSavedUsersAsync()
    {
        try
        {
            var usersJson = await localStorageService.GetItemAsync(SavedUsersStorageKey);

            if (!string.IsNullOrEmpty(usersJson))
            {
                var usersData = JsonSerializer.Deserialize<SavedUsersData>(usersJson);
                if (usersData?.Users != null && usersData.Users.Count > 0)
                {
                    SavedUsers = usersData.Users;
                    ShowUsersList = true;
                    ShowRememberMe = false;
                }
            }
        }
        catch
        {
            SavedUsers = [];
            ShowUsersList = false;
            ShowRememberMe = true;
        }
    }

    private async Task SaveUserAsync(string email, WebAuthnStoredCredential? webAuthnCredential = null)
    {
        try
        {
            var existingUser = savedUsers.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

            if (existingUser != null)
            {
                if (webAuthnCredential != null)
                {
                    existingUser.WebAuthnCredential = webAuthnCredential;
                }
            }
            else
            {
                savedUsers.Add(new SavedUser { Email = email, WebAuthnCredential = webAuthnCredential });
            }

            var usersData = new SavedUsersData { Users = savedUsers };
            await localStorageService.SetItemAsync(SavedUsersStorageKey, JsonSerializer.Serialize(usersData));
        }
        catch
        {
            // Continue silently
        }
    }

    private async Task AttemptWebAuthnLoginAsync(SavedUser user)
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var authResult = await webAuthnService.AuthenticateAsync(user.WebAuthnCredential!.CredentialID);

            if (authResult.Success && !string.IsNullOrEmpty(authResult.PrfOutput))
            {
                var decryptedPassword = await DecryptPasswordWithPrfAsync(
                    user.WebAuthnCredential.EncryptedPassword!,
                    authResult.PrfOutput);

                var loginResult = await authService.Login(user.Email, decryptedPassword);

                if (loginResult.Success)
                {
                    navigationService.NavigateTo("/");
                }
                else
                {
                    ErrorMessage = "Biometric login failed. Please enter your password manually.";
                    FallbackToManualLogin(user);
                }
            }
            else
            {
                ErrorMessage = authResult.Error ?? "Biometric authentication failed. Please enter your password manually.";
                FallbackToManualLogin(user);
            }
        }
        catch
        {
            ErrorMessage = "Biometric login error. Please enter your password manually.";
            FallbackToManualLogin(user);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void FallbackToManualLogin(SavedUser user)
    {
        Username = user.Email;
        Password = string.Empty;
        IsUsernameReadonly = false;
        ShowUsersList = false;
        ShowRememberMe = false;
    }

    private async Task<string> EncryptPasswordWithPrfAsync(string password, string prfOutputHex)
    {
        var prfBytes = Convert.FromHexString(prfOutputHex);
        var base64Key = Convert.ToBase64String(prfBytes);
        var passwordBytes = System.Text.Encoding.UTF8.GetBytes(password);

        using var inputStream = new MemoryStream(passwordBytes);
        using var outputStream = new MemoryStream();
        await cryptoService.EncryptAsync(inputStream, outputStream, base64Key);
        return Convert.ToBase64String(outputStream.ToArray());
    }

    private async Task<string> DecryptPasswordWithPrfAsync(string encryptedPasswordBase64, string prfOutputHex)
    {
        var prfBytes = Convert.FromHexString(prfOutputHex);
        var base64Key = Convert.ToBase64String(prfBytes);
        var encryptedBytes = Convert.FromBase64String(encryptedPasswordBase64);

        using var inputStream = new MemoryStream(encryptedBytes);
        using var outputStream = new MemoryStream();
        await cryptoService.DecryptAsync(inputStream, outputStream, base64Key);
        return System.Text.Encoding.UTF8.GetString(outputStream.ToArray());
    }
}
