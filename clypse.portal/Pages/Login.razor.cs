using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using clypse.portal.Services;
using clypse.portal.Models;
using System.Text.Json;
using clypse.core.Cryptogtaphy.Interfaces;

namespace clypse.portal.Pages;

public partial class Login : ComponentBase
{
    [Inject] public IAuthenticationService AuthService { get; set; } = default!;
    [Inject] public NavigationManager Navigation { get; set; } = default!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] public AppSettings AppSettings { get; set; } = default!;
    [Inject] public ICryptoService CryptoService { get; set; } = default!;
    [Inject] public IUserSettingsService UserSettingsService { get; set; } = default!;

    private LoginModel loginModel = new();
    private bool isLoading = false;
    private string? errorMessage;
    private string currentTheme = "light";
    private string themeIcon = "bi-moon";
    
    // User management properties
    private List<SavedUser> savedUsers = new();
    private bool showUsersList = false;
    private bool showRememberMe = true;
    private bool rememberMe = false;
    private bool isUsernameReadonly = false;
    private bool passwordResetRequired = false;
    private bool rememberMeWhenResetStarted = false;
    private string newPassword = string.Empty;
    private string confirmPassword = string.Empty;
    
    // WebAuthn properties
    private bool showWebAuthnPrompt = false;
    private bool isWebAuthnProcessing = false;
    private string? webAuthnErrorMessage;

    private class LoginModel
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    private class SavedUser
    {
        public string Email { get; set; } = string.Empty;
        public WebAuthnCredential? WebAuthnCredential { get; set; }
    }

    private class WebAuthnCredential
    {
        public string CredentialID { get; set; } = string.Empty;
        public string UserID { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? EncryptedPassword { get; set; }
    }

    private class SavedUsersData
    {
        public List<SavedUser> Users { get; set; } = new();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await AuthService.Initialize();
            await InitializeTheme();
            await LoadSavedUsers();
            
            // Check if already authenticated
            if (await AuthService.CheckAuthentication())
            {
                Navigation.NavigateTo("/");
            }
        }
    }

    private async Task InitializeTheme()
    {
        try
        {
            currentTheme = await UserSettingsService.GetThemeAsync();
            themeIcon = currentTheme == "light" ? "bi-moon" : "bi-sun";
            
            await JSRuntime.InvokeVoidAsync("document.documentElement.setAttribute", "data-theme", currentTheme);
        }
        catch
        {
            currentTheme = "light";
            themeIcon = "bi-moon";
        }
    }

    private async Task ToggleTheme()
    {
        currentTheme = currentTheme == "light" ? "dark" : "light";
        themeIcon = currentTheme == "light" ? "bi-moon" : "bi-sun";
        
        await UserSettingsService.SetThemeAsync(currentTheme);
        await JSRuntime.InvokeVoidAsync("document.documentElement.setAttribute", "data-theme", currentTheme);
        
        StateHasChanged();
    }

    private async Task LoadSavedUsers()
    {
        try
        {
            var usersJson = await JSRuntime.InvokeAsync<string?>("localStorage.getItem", "users");
            
            if (!string.IsNullOrEmpty(usersJson))
            {
                var usersData = System.Text.Json.JsonSerializer.Deserialize<SavedUsersData>(usersJson);
                if (usersData?.Users != null && usersData.Users.Count > 0)
                {
                    savedUsers = usersData.Users;
                    showUsersList = true;
                    showRememberMe = false;
                }
            }
            
            StateHasChanged();
        }
        catch (Exception)
        {
            // If there's an error loading users, continue with normal login flow
            savedUsers = new List<SavedUser>();
            showUsersList = false;
            showRememberMe = true;
        }
    }

    private async Task SaveUser(string email, WebAuthnCredential? webAuthnCredential = null)
    {
        try
        {
            // Check if user already exists
            var existingUser = savedUsers.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
            
            if (existingUser != null)
            {
                // Update existing user with WebAuthn credential if provided
                if (webAuthnCredential != null)
                {
                    existingUser.WebAuthnCredential = webAuthnCredential;
                }
            }
            else
            {
                // Add new user
                var userToAdd = new SavedUser 
                { 
                    Email = email,
                    WebAuthnCredential = webAuthnCredential
                };
                savedUsers.Add(userToAdd);
            }
            
            var usersData = new SavedUsersData { Users = savedUsers };
            var usersJson = System.Text.Json.JsonSerializer.Serialize(usersData);
            
            await JSRuntime.InvokeVoidAsync("localStorage.setItem", "users", usersJson);
        }
        catch (Exception)
        {
            // If there's an error saving, continue silently
        }
    }

    private async Task SelectUser(SavedUser user)
    {
        // Check if user has WebAuthn credential for biometric login
        if (user.WebAuthnCredential != null && !string.IsNullOrEmpty(user.WebAuthnCredential.EncryptedPassword))
        {
            await AttemptWebAuthnLogin(user);
        }
        else
        {
            // Traditional flow: show login form with username pre-populated
            loginModel.Username = user.Email;
            isUsernameReadonly = false;
            showUsersList = false;
            showRememberMe = false;
            StateHasChanged();
        }
    }

    private async Task AttemptWebAuthnLogin(SavedUser user)
    {
        isLoading = true;
        errorMessage = null;
        StateHasChanged();

        try
        {
            // Attempt WebAuthn authentication
            var authResult = await JSRuntime.InvokeAsync<WebAuthnAuthenticateResult>(
                "webAuthnWrapper.authenticate", 
                user.WebAuthnCredential!.CredentialID);

            if (authResult.Success && !string.IsNullOrEmpty(authResult.PrfOutput))
            {
                // Decrypt the stored password using PRF output
                var decryptedPassword = await DecryptPasswordWithPrf(
                    user.WebAuthnCredential.EncryptedPassword!, 
                    authResult.PrfOutput);

                // Attempt login with decrypted credentials
                var loginResult = await AuthService.Login(user.Email, decryptedPassword);

                if (loginResult.Success)
                {
                    // Successful biometric login - navigate to app
                    Navigation.NavigateTo("/");
                }
                else
                {
                    // Login failed - fall back to manual login
                    errorMessage = "Biometric login failed. Please enter your password manually.";
                    FallbackToManualLogin(user);
                }
            }
            else
            {
                // WebAuthn authentication failed - fall back to manual login
                errorMessage = authResult.Error ?? "Biometric authentication failed. Please enter your password manually.";
                FallbackToManualLogin(user);
            }
        }
        catch (Exception)
        {
            // Any error - fall back to manual login
            errorMessage = "Biometric login error. Please enter your password manually.";
            FallbackToManualLogin(user);
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    private void FallbackToManualLogin(SavedUser user)
    {
        // Show traditional login form
        loginModel.Username = user.Email;
        loginModel.Password = string.Empty;
        isUsernameReadonly = false;
        showUsersList = false;
        showRememberMe = false;
    }

    private void ShowLoginForm()
    {
        showUsersList = false;
        showRememberMe = true;
        isUsernameReadonly = false;
        loginModel.Username = string.Empty;
        loginModel.Password = string.Empty;
        rememberMe = false;
        
        StateHasChanged();
    }

    private void ShowUsersList()
    {
        showUsersList = true;
        showRememberMe = false;
        loginModel.Username = string.Empty;
        loginModel.Password = string.Empty;
        rememberMe = false;
        
        StateHasChanged();
    }

    private async Task RemoveUser(SavedUser user)
    {
        try
        {
            // Remove user from the list
            savedUsers.Remove(user);
            
            // Update localStorage
            var usersData = new SavedUsersData { Users = savedUsers };
            var usersJson = System.Text.Json.JsonSerializer.Serialize(usersData);
            await JSRuntime.InvokeVoidAsync("localStorage.setItem", "users", usersJson);
            
            // If no users left, switch to normal login form
            if (!savedUsers.Any())
            {
                showUsersList = false;
                showRememberMe = true;
            }
            
            StateHasChanged();
        }
        catch (Exception)
        {
            // If there's an error, continue silently
        }
    }

    private async Task HandleLogin()
    {
        isLoading = true;
        errorMessage = null;
        StateHasChanged();

        try
        {
            var result = await AuthService.Login(loginModel.Username, loginModel.Password);

            if (result.Success)
            {
                // Save user if remember me is checked and login was successful
                if (rememberMe && !string.IsNullOrEmpty(loginModel.Username))
                {
                    await SaveUser(loginModel.Username);
                    
                    // Show WebAuthn setup prompt when remember me is checked AND portal login auth is enabled
                    if (AppSettings.EnablePortalLoginAuthn)
                    {
                        showWebAuthnPrompt = true;
                    }
                    else
                    {
                        Navigation.NavigateTo("/");
                    }
                }
                else
                {
                    Navigation.NavigateTo("/");
                }
            }
            else if (result.PasswordResetRequired)
            {
                passwordResetRequired = true;
                rememberMeWhenResetStarted = rememberMe; // Preserve the remember me choice
                errorMessage = result.Error ?? "Password reset required";
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

    private async Task HandlePasswordChange()
    {
        if (string.IsNullOrEmpty(newPassword) || newPassword != confirmPassword)
        {
            errorMessage = "Passwords do not match";
            StateHasChanged();
            return;
        }

        isLoading = true;
        errorMessage = null;
        StateHasChanged();

        try
        {
            var result = await AuthService.CompletePasswordReset(loginModel.Username, newPassword);

            if (result.Success)
            {
                // Save user if remember me was checked before password reset was triggered
                if (rememberMeWhenResetStarted && !string.IsNullOrEmpty(loginModel.Username))
                {
                    await SaveUser(loginModel.Username);
                    
                    // Show WebAuthn setup prompt when remember me was originally checked AND portal login auth is enabled
                    if (AppSettings.EnablePortalLoginAuthn)
                    {
                        showWebAuthnPrompt = true;
                    }
                    else
                    {
                        Navigation.NavigateTo("/");
                    }
                }
                else
                {
                    Navigation.NavigateTo("/");
                }
            }
            else
            {
                errorMessage = result.Error ?? "Failed to change password";
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

    private async Task HandleWebAuthnSetup()
    {
        isWebAuthnProcessing = true;
        webAuthnErrorMessage = null;
        StateHasChanged();

        try
        {
            var result = await JSRuntime.InvokeAsync<WebAuthnRegisterResult>("webAuthnWrapper.register", 
                loginModel.Username, null);

            if (result.Success && result.PrfEnabled)
            {
                // Registration successful with PRF - encrypt password and store credential
                string? encryptedPassword = null;
                
                if (!string.IsNullOrEmpty(result.PrfOutput))
                {
                    try
                    {
                        encryptedPassword = await EncryptPasswordWithPrf(loginModel.Password, result.PrfOutput);
                    }
                    catch (Exception ex)
                    {
                        // Log the error but don't fail the entire process
                        Console.WriteLine($"Failed to encrypt password: {ex.Message}");
                    }
                }
                
                var webAuthnCredential = new WebAuthnCredential
                {
                    CredentialID = result.CredentialID ?? string.Empty,
                    UserID = result.UserID ?? string.Empty,
                    CreatedAt = DateTime.UtcNow,
                    EncryptedPassword = encryptedPassword
                };
                
                await SaveUser(loginModel.Username, webAuthnCredential);
                Navigation.NavigateTo("/");
            }
            else if (result.Success && !result.PrfEnabled)
            {
                webAuthnErrorMessage = "Your authenticator doesn't support the required security features for biometric login. Please continue with regular login.";
            }
            else
            {
                webAuthnErrorMessage = result.Error ?? "Failed to set up biometric login. Please try again or continue with regular login.";
            }
        }
        catch (Exception ex)
        {
            webAuthnErrorMessage = $"Error setting up biometric login: {ex.Message}";
        }
        finally
        {
            isWebAuthnProcessing = false;
            StateHasChanged();
        }
    }

    private void SkipWebAuthnSetup()
    {
        showWebAuthnPrompt = false;
        Navigation.NavigateTo("/");
    }

    private void DismissWebAuthnError()
    {
        webAuthnErrorMessage = null;
        showWebAuthnPrompt = false;
        Navigation.NavigateTo("/");
    }

    private class WebAuthnRegisterResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public string? CredentialID { get; set; }
        public string? UserID { get; set; }
        public string? Username { get; set; }
        public bool PrfEnabled { get; set; }
        public string? PrfOutput { get; set; }
    }

    private class WebAuthnAuthenticateResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public bool UserPresent { get; set; }
        public bool UserVerified { get; set; }
        public string? PrfOutput { get; set; }
    }

    private async Task<string> EncryptPasswordWithPrf(string password, string prfOutputHex)
    {
        // Convert hex PRF output to bytes then to base64 key
        var prfBytes = Convert.FromHexString(prfOutputHex);
        var base64Key = Convert.ToBase64String(prfBytes);
        
        // Encrypt the password
        var passwordBytes = System.Text.Encoding.UTF8.GetBytes(password);
        using var inputStream = new MemoryStream(passwordBytes);
        using var outputStream = new MemoryStream();
        
        await CryptoService.EncryptAsync(inputStream, outputStream, base64Key);
        
        // Return encrypted password as base64
        return Convert.ToBase64String(outputStream.ToArray());
    }

    private async Task<string> DecryptPasswordWithPrf(string encryptedPasswordBase64, string prfOutputHex)
    {
        // Convert hex PRF output to bytes then to base64 key
        var prfBytes = Convert.FromHexString(prfOutputHex);
        var base64Key = Convert.ToBase64String(prfBytes);
        
        // Decrypt the password
        var encryptedBytes = Convert.FromBase64String(encryptedPasswordBase64);
        using var inputStream = new MemoryStream(encryptedBytes);
        using var outputStream = new MemoryStream();
        
        await CryptoService.DecryptAsync(inputStream, outputStream, base64Key);
        
        // Return decrypted password
        return System.Text.Encoding.UTF8.GetString(outputStream.ToArray());
    }
}
