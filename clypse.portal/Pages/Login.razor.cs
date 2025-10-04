using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using clypse.portal.Services;
using clypse.portal.Models;
using System.Text.Json;

namespace clypse.portal.Pages;

public partial class Login : ComponentBase
{
    [Inject] public IAuthenticationService AuthService { get; set; } = default!;
    [Inject] public NavigationManager Navigation { get; set; } = default!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] public AppSettings AppSettings { get; set; } = default!;

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

    private class LoginModel
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    private class SavedUser
    {
        public string Email { get; set; } = string.Empty;
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
            var savedTheme = await JSRuntime.InvokeAsync<string>("localStorage.getItem", "clypse_theme");
            currentTheme = !string.IsNullOrEmpty(savedTheme) ? savedTheme : "light";
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
        
        await JSRuntime.InvokeVoidAsync("localStorage.setItem", "clypse_theme", currentTheme);
        await JSRuntime.InvokeVoidAsync("document.documentElement.setAttribute", "data-theme", currentTheme);
        
        StateHasChanged();
    }

    private async Task LoadSavedUsers()
    {
        try
        {
            var usersJson = await JSRuntime.InvokeAsync<string?>("localStorage.getItem", "users.json");
            
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

    private async Task SaveUser(string email)
    {
        try
        {
            var userToAdd = new SavedUser { Email = email };
            
            // Check if user already exists
            if (!savedUsers.Any(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
            {
                savedUsers.Add(userToAdd);
                
                var usersData = new SavedUsersData { Users = savedUsers };
                var usersJson = System.Text.Json.JsonSerializer.Serialize(usersData);
                
                await JSRuntime.InvokeVoidAsync("localStorage.setItem", "users.json", usersJson);
            }
        }
        catch (Exception)
        {
            // If there's an error saving, continue silently
        }
    }

    private void SelectUser(SavedUser user)
    {
        loginModel.Username = user.Email;
        isUsernameReadonly = false;
        showUsersList = false;
        showRememberMe = false;
        
        StateHasChanged();
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
            await JSRuntime.InvokeVoidAsync("localStorage.setItem", "users.json", usersJson);
            
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
                }
                
                Navigation.NavigateTo("/");
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
                }
                
                Navigation.NavigateTo("/");
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
}
