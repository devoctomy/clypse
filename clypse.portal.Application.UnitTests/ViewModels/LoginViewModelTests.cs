using System.Text.Json;
using clypse.core.Cryptography.Interfaces;
using clypse.portal.Application.Services.Interfaces;
using clypse.portal.Application.ViewModels;
using clypse.portal.Models.Login;
using clypse.portal.Models.Settings;
using clypse.portal.Models.WebAuthn;
using Moq;

namespace clypse.portal.Application.UnitTests.ViewModels;

public class LoginViewModelTests
{
    private readonly Mock<IAuthenticationService> mockAuthService;
    private readonly Mock<INavigationService> mockNavigationService;
    private readonly Mock<IUserSettingsService> mockUserSettingsService;
    private readonly Mock<IBrowserInteropService> mockBrowserInteropService;
    private readonly Mock<ILocalStorageService> mockLocalStorageService;
    private readonly Mock<IWebAuthnService> mockWebAuthnService;
    private readonly Mock<ICryptoService> mockCryptoService;
    private readonly AppSettings appSettings;

    public LoginViewModelTests()
    {
        this.mockAuthService = new Mock<IAuthenticationService>();
        this.mockNavigationService = new Mock<INavigationService>();
        this.mockUserSettingsService = new Mock<IUserSettingsService>();
        this.mockBrowserInteropService = new Mock<IBrowserInteropService>();
        this.mockLocalStorageService = new Mock<ILocalStorageService>();
        this.mockWebAuthnService = new Mock<IWebAuthnService>();
        this.mockCryptoService = new Mock<ICryptoService>();
        this.appSettings = new AppSettings();

        this.mockUserSettingsService.Setup(s => s.GetThemeAsync()).ReturnsAsync("light");
        this.mockUserSettingsService.Setup(s => s.SetThemeAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        this.mockBrowserInteropService.Setup(s => s.SetThemeAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        this.mockLocalStorageService.Setup(s => s.GetItemAsync(It.IsAny<string>())).ReturnsAsync((string?)null);
        this.mockLocalStorageService.Setup(s => s.SetItemAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
        this.mockAuthService.Setup(s => s.Initialize()).Returns(Task.CompletedTask);
    }

    private LoginViewModel CreateSut()
    {
        return new LoginViewModel(
            this.mockAuthService.Object,
            this.mockNavigationService.Object,
            this.mockUserSettingsService.Object,
            this.mockBrowserInteropService.Object,
            this.mockLocalStorageService.Object,
            this.mockWebAuthnService.Object,
            this.mockCryptoService.Object,
            this.appSettings);
    }

    // --- Constructor ---

    [Fact]
    public void GivenValidParameters_WhenConstructing_ThenCreatesInstance()
    {
        // Act
        var sut = CreateSut();

        // Assert
        Assert.NotNull(sut);
    }

    [Fact]
    public void GivenNullAuthService_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Arrange / Act / Assert
        Assert.Throws<ArgumentNullException>(() => new LoginViewModel(
            null!,
            this.mockNavigationService.Object,
            this.mockUserSettingsService.Object,
            this.mockBrowserInteropService.Object,
            this.mockLocalStorageService.Object,
            this.mockWebAuthnService.Object,
            this.mockCryptoService.Object,
            this.appSettings));
    }

    [Fact]
    public void GivenNullNavigationService_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Arrange / Act / Assert
        Assert.Throws<ArgumentNullException>(() => new LoginViewModel(
            this.mockAuthService.Object,
            null!,
            this.mockUserSettingsService.Object,
            this.mockBrowserInteropService.Object,
            this.mockLocalStorageService.Object,
            this.mockWebAuthnService.Object,
            this.mockCryptoService.Object,
            this.appSettings));
    }

    // --- Initial state ---

    [Fact]
    public void GivenNewInstance_WhenCheckingInitialState_ThenDefaultValuesAreCorrect()
    {
        // Arrange
        var sut = CreateSut();

        // Assert
        Assert.False(sut.IsLoading);
        Assert.Null(sut.ErrorMessage);
        Assert.Equal("light", sut.CurrentTheme);
        Assert.Equal("bi-moon", sut.ThemeIcon);
        Assert.False(sut.ShowUsersList);
        Assert.True(sut.ShowRememberMe);
        Assert.False(sut.RememberMe);
        Assert.False(sut.PasswordResetRequired);
        Assert.Empty(sut.Username);
        Assert.Empty(sut.Password);
    }

    // --- AppSettings property ---

    [Fact]
    public void GivenInstance_WhenGetAppSettings_ThenReturnsCorrectSettings()
    {
        // Arrange
        var sut = CreateSut();

        // Assert
        Assert.Same(this.appSettings, sut.AppSettings);
    }

    // --- OnAfterRenderAsync ---

    [Fact]
    public async Task GivenFirstRenderAndNotAuthenticated_WhenOnAfterRenderAsync_ThenNoNavigation()
    {
        // Arrange
        var sut = CreateSut();
        this.mockAuthService.Setup(s => s.CheckAuthentication()).ReturnsAsync(false);

        // Act
        await sut.OnAfterRenderAsync(firstRender: true);

        // Assert
        this.mockNavigationService.Verify(n => n.NavigateTo(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GivenFirstRenderAndAuthenticated_WhenOnAfterRenderAsync_ThenNavigatesToHome()
    {
        // Arrange
        var sut = CreateSut();
        this.mockAuthService.Setup(s => s.CheckAuthentication()).ReturnsAsync(true);

        // Act
        await sut.OnAfterRenderAsync(firstRender: true);

        // Assert
        this.mockNavigationService.Verify(n => n.NavigateTo("/"), Times.Once);
    }

    [Fact]
    public async Task GivenFirstRenderWithSavedUsers_WhenOnAfterRenderAsync_ThenShowUsersListIsTrue()
    {
        // Arrange
        var sut = CreateSut();
        var usersData = new SavedUsersData
        {
            Users = new List<SavedUser> { new() { Email = "user@test.com" } },
        };
        this.mockLocalStorageService
            .Setup(s => s.GetItemAsync("users"))
            .ReturnsAsync(JsonSerializer.Serialize(usersData));
        this.mockAuthService.Setup(s => s.CheckAuthentication()).ReturnsAsync(false);

        // Act
        await sut.OnAfterRenderAsync(firstRender: true);

        // Assert
        Assert.True(sut.ShowUsersList);
        Assert.False(sut.ShowRememberMe);
    }

    [Fact]
    public async Task GivenNotFirstRender_WhenOnAfterRenderAsync_ThenAuthNotChecked()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.OnAfterRenderAsync(firstRender: false);

        // Assert
        this.mockAuthService.Verify(s => s.Initialize(), Times.Never);
    }

    // --- ToggleThemeAsync ---

    [Fact]
    public async Task GivenCurrentThemeIsLight_WhenToggleTheme_ThenThemeChangesToDark()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.ToggleThemeCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal("dark", sut.CurrentTheme);
        Assert.Equal("bi-sun", sut.ThemeIcon);
    }

    [Fact]
    public async Task GivenCurrentThemeIsDark_WhenToggleTheme_ThenThemeChangesToLight()
    {
        // Arrange
        var sut = CreateSut();
        sut.CurrentTheme = "dark";
        sut.ThemeIcon = "bi-sun";

        // Act
        await sut.ToggleThemeCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal("light", sut.CurrentTheme);
        Assert.Equal("bi-moon", sut.ThemeIcon);
    }

    // --- SelectUserAsync (no WebAuthn) ---

    [Fact]
    public async Task GivenUserWithNoWebAuthn_WhenSelectUserAsync_ThenUsernameIsSetAndListHidden()
    {
        // Arrange
        var sut = CreateSut();
        var user = new SavedUser { Email = "user@test.com", WebAuthnCredential = null };

        // Act
        await sut.SelectUserCommand.ExecuteAsync(user);

        // Assert
        Assert.Equal("user@test.com", sut.Username);
        Assert.False(sut.ShowUsersList);
        Assert.False(sut.ShowRememberMe);
    }

    // --- SelectUserAsync (with WebAuthn, failed authenticate) ---

    [Fact]
    public async Task GivenUserWithWebAuthnAndAuthFails_WhenSelectUserAsync_ThenFallsBackToManualLogin()
    {
        // Arrange
        var sut = CreateSut();
        var user = new SavedUser
        {
            Email = "user@test.com",
            WebAuthnCredential = new WebAuthnStoredCredential { CredentialID = "cred1", EncryptedPassword = "enc" },
        };
        this.mockWebAuthnService
            .Setup(w => w.AuthenticateAsync("cred1"))
            .ReturnsAsync(new WebAuthnAuthenticateResult { Success = false, Error = "auth failed" });

        // Act
        await sut.SelectUserCommand.ExecuteAsync(user);

        // Assert
        Assert.Equal("user@test.com", sut.Username);
        Assert.False(sut.ShowUsersList);
        Assert.NotNull(sut.ErrorMessage);
    }

    // --- RemoveUserAsync ---

    [Fact]
    public async Task GivenExistingUser_WhenRemoveUserAsync_ThenUserIsRemovedFromList()
    {
        // Arrange
        var sut = CreateSut();
        var usersData = new SavedUsersData
        {
            Users = new List<SavedUser>
            {
                new() { Email = "user1@test.com" },
                new() { Email = "user2@test.com" },
            },
        };
        this.mockLocalStorageService
            .Setup(s => s.GetItemAsync("users"))
            .ReturnsAsync(JsonSerializer.Serialize(usersData));
        this.mockAuthService.Setup(s => s.CheckAuthentication()).ReturnsAsync(false);
        await sut.OnAfterRenderAsync(firstRender: true);
        var user = sut.SavedUsers[0];

        // Act
        await sut.RemoveUserCommand.ExecuteAsync(user);

        // Assert
        Assert.Single(sut.SavedUsers);
    }

    [Fact]
    public async Task GivenLastUser_WhenRemoveUserAsync_ThenUsersListIsHidden()
    {
        // Arrange
        var sut = CreateSut();
        var usersData = new SavedUsersData
        {
            Users = new List<SavedUser> { new() { Email = "only@test.com" } },
        };
        this.mockLocalStorageService
            .Setup(s => s.GetItemAsync("users"))
            .ReturnsAsync(JsonSerializer.Serialize(usersData));
        this.mockAuthService.Setup(s => s.CheckAuthentication()).ReturnsAsync(false);
        await sut.OnAfterRenderAsync(firstRender: true);
        var user = sut.SavedUsers[0];

        // Act
        await sut.RemoveUserCommand.ExecuteAsync(user);

        // Assert
        Assert.Empty(sut.SavedUsers);
        Assert.False(sut.ShowUsersList);
        Assert.True(sut.ShowRememberMe);
    }

    // --- ShowLoginForm / ShowUsersListCommand ---

    [Fact]
    public void GivenUsersList_WhenShowLoginForm_ThenUsersListIsHidden()
    {
        // Arrange
        var sut = CreateSut();
        sut.ShowUsersList = true;

        // Act
        sut.ShowLoginFormCommand.Execute(null);

        // Assert
        Assert.False(sut.ShowUsersList);
        Assert.True(sut.ShowRememberMe);
        Assert.Empty(sut.Username);
        Assert.Empty(sut.Password);
    }

    [Fact]
    public void GivenLoginForm_WhenShowUsersListCommand_ThenUsersListIsShown()
    {
        // Arrange
        var sut = CreateSut();
        sut.ShowUsersList = false;

        // Act
        sut.ShowUsersListCommandCommand.Execute(null);

        // Assert
        Assert.True(sut.ShowUsersList);
        Assert.False(sut.ShowRememberMe);
    }

    // --- HandleLoginAsync ---

    [Fact]
    public async Task GivenValidCredentials_WhenHandleLogin_ThenNavigatesToHome()
    {
        // Arrange
        var sut = CreateSut();
        sut.Username = "testuser";
        sut.Password = "testpass";
        this.mockAuthService.Setup(s => s.Login("testuser", "testpass"))
            .ReturnsAsync(new LoginResult { Success = true });

        // Act
        await sut.HandleLoginCommand.ExecuteAsync(null);

        // Assert
        this.mockNavigationService.Verify(n => n.NavigateTo("/"), Times.Once);
        Assert.Null(sut.ErrorMessage);
    }

    [Fact]
    public async Task GivenInvalidCredentials_WhenHandleLogin_ThenErrorMessageIsSet()
    {
        // Arrange
        var sut = CreateSut();
        sut.Username = "testuser";
        sut.Password = "wrongpass";
        this.mockAuthService.Setup(s => s.Login("testuser", "wrongpass"))
            .ReturnsAsync(new LoginResult { Success = false, Error = "Invalid credentials" });

        // Act
        await sut.HandleLoginCommand.ExecuteAsync(null);

        // Assert
        this.mockNavigationService.Verify(n => n.NavigateTo(It.IsAny<string>()), Times.Never);
        Assert.Equal("Invalid credentials", sut.ErrorMessage);
    }

    [Fact]
    public async Task GivenPasswordResetRequired_WhenHandleLogin_ThenPasswordResetFlagIsSet()
    {
        // Arrange
        var sut = CreateSut();
        sut.Username = "testuser";
        sut.Password = "oldpass";
        this.mockAuthService.Setup(s => s.Login("testuser", "oldpass"))
            .ReturnsAsync(new LoginResult { Success = false, PasswordResetRequired = true, Error = "Password reset required" });

        // Act
        await sut.HandleLoginCommand.ExecuteAsync(null);

        // Assert
        Assert.True(sut.PasswordResetRequired);
        Assert.Equal("Password reset required", sut.ErrorMessage);
    }

    [Fact]
    public async Task GivenLoginThrows_WhenHandleLogin_ThenErrorMessageIsSetAndIsLoadingResets()
    {
        // Arrange
        var sut = CreateSut();
        sut.Username = "testuser";
        sut.Password = "testpass";
        this.mockAuthService.Setup(s => s.Login("testuser", "testpass"))
            .ThrowsAsync(new Exception("network error"));

        // Act
        await sut.HandleLoginCommand.ExecuteAsync(null);

        // Assert
        Assert.NotNull(sut.ErrorMessage);
        Assert.False(sut.IsLoading);
    }

    [Fact]
    public async Task GivenRememberMeAndSuccessfulLogin_WhenHandleLogin_ThenSaveUserIsCalled()
    {
        // Arrange
        var sut = CreateSut();
        sut.Username = "testuser";
        sut.Password = "testpass";
        sut.RememberMe = true;
        this.mockAuthService.Setup(s => s.Login("testuser", "testpass"))
            .ReturnsAsync(new LoginResult { Success = true });

        // Act
        await sut.HandleLoginCommand.ExecuteAsync(null);

        // Assert
        this.mockLocalStorageService.Verify(
            s => s.SetItemAsync("users", It.IsAny<string>()),
            Times.Once);
    }

    // --- HandlePasswordChangeAsync ---

    [Fact]
    public async Task GivenMismatchedPasswords_WhenHandlePasswordChange_ThenErrorIsSet()
    {
        // Arrange
        var sut = CreateSut();
        sut.NewPassword = "newpass1";
        sut.ConfirmPassword = "newpass2";

        // Act
        await sut.HandlePasswordChangeCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal("Passwords do not match", sut.ErrorMessage);
    }

    [Fact]
    public async Task GivenMatchingPasswordsAndSuccessReset_WhenHandlePasswordChange_ThenNavigatesToHome()
    {
        // Arrange
        var sut = CreateSut();
        sut.Username = "user";
        sut.NewPassword = "newpass";
        sut.ConfirmPassword = "newpass";
        this.mockAuthService
            .Setup(s => s.CompletePasswordReset("user", "newpass"))
            .ReturnsAsync(new LoginResult { Success = true });

        // Act
        await sut.HandlePasswordChangeCommand.ExecuteAsync(null);

        // Assert
        this.mockNavigationService.Verify(n => n.NavigateTo("/"), Times.Once);
        Assert.False(sut.IsLoading);
    }

    [Fact]
    public async Task GivenMatchingPasswordsAndFailedReset_WhenHandlePasswordChange_ThenErrorIsSet()
    {
        // Arrange
        var sut = CreateSut();
        sut.Username = "user";
        sut.NewPassword = "newpass";
        sut.ConfirmPassword = "newpass";
        this.mockAuthService
            .Setup(s => s.CompletePasswordReset("user", "newpass"))
            .ReturnsAsync(new LoginResult { Success = false, Error = "Reset failed" });

        // Act
        await sut.HandlePasswordChangeCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal("Reset failed", sut.ErrorMessage);
        Assert.False(sut.IsLoading);
    }

    [Fact]
    public async Task GivenPasswordChangeThrows_WhenHandlePasswordChange_ThenErrorIsSet()
    {
        // Arrange
        var sut = CreateSut();
        sut.Username = "user";
        sut.NewPassword = "newpass";
        sut.ConfirmPassword = "newpass";
        this.mockAuthService
            .Setup(s => s.CompletePasswordReset(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("reset error"));

        // Act
        await sut.HandlePasswordChangeCommand.ExecuteAsync(null);

        // Assert
        Assert.NotNull(sut.ErrorMessage);
        Assert.False(sut.IsLoading);
    }

    // --- HandleWebAuthnSetupAsync ---

    [Fact]
    public async Task GivenSuccessfulRegisterWithPrf_WhenHandleWebAuthnSetup_ThenNavigatesToHome()
    {
        // Arrange
        var sut = CreateSut();
        sut.Username = "user";
        sut.Password = "pass";
        var prfBytes = new byte[32];
        var prfHex = Convert.ToHexString(prfBytes);
        this.mockWebAuthnService
            .Setup(w => w.RegisterAsync("user", null))
            .ReturnsAsync(new WebAuthnRegisterResult { Success = true, PrfEnabled = true, PrfOutput = prfHex, CredentialID = "cred1", UserID = "uid1" });
        this.mockCryptoService
            .Setup(c => c.EncryptAsync(It.IsAny<Stream>(), It.IsAny<Stream>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await sut.HandleWebAuthnSetupCommand.ExecuteAsync(null);

        // Assert
        this.mockNavigationService.Verify(n => n.NavigateTo("/"), Times.Once);
        Assert.False(sut.IsWebAuthnProcessing);
    }

    [Fact]
    public async Task GivenSuccessfulRegisterWithoutPrf_WhenHandleWebAuthnSetup_ThenErrorMessageIsSet()
    {
        // Arrange
        var sut = CreateSut();
        sut.Username = "user";
        this.mockWebAuthnService
            .Setup(w => w.RegisterAsync("user", null))
            .ReturnsAsync(new WebAuthnRegisterResult { Success = true, PrfEnabled = false });

        // Act
        await sut.HandleWebAuthnSetupCommand.ExecuteAsync(null);

        // Assert
        Assert.NotNull(sut.WebAuthnErrorMessage);
        Assert.False(sut.IsWebAuthnProcessing);
    }

    [Fact]
    public async Task GivenRegisterThrows_WhenHandleWebAuthnSetup_ThenErrorMessageIsSet()
    {
        // Arrange
        var sut = CreateSut();
        sut.Username = "user";
        this.mockWebAuthnService
            .Setup(w => w.RegisterAsync(It.IsAny<string>(), null))
            .ThrowsAsync(new Exception("webauthn error"));

        // Act
        await sut.HandleWebAuthnSetupCommand.ExecuteAsync(null);

        // Assert
        Assert.NotNull(sut.WebAuthnErrorMessage);
        Assert.False(sut.IsWebAuthnProcessing);
    }

    // --- SkipWebAuthnSetup ---

    [Fact]
    public void GivenWebAuthnSetup_WhenSkipWebAuthnSetup_ThenNavigatesToHome()
    {
        // Arrange
        var sut = CreateSut();
        sut.ShowWebAuthnPrompt = true;

        // Act
        sut.SkipWebAuthnSetupCommand.Execute(null);

        // Assert
        Assert.False(sut.ShowWebAuthnPrompt);
        this.mockNavigationService.Verify(n => n.NavigateTo("/"), Times.Once);
    }

    // --- DismissWebAuthnError ---

    [Fact]
    public void GivenWebAuthnError_WhenDismissWebAuthnError_ThenErrorIsClearedAndNavigatesToHome()
    {
        // Arrange
        var sut = CreateSut();
        sut.WebAuthnErrorMessage = "Some error";
        sut.ShowWebAuthnPrompt = true;

        // Act
        sut.DismissWebAuthnErrorCommand.Execute(null);

        // Assert
        Assert.Null(sut.WebAuthnErrorMessage);
        Assert.False(sut.ShowWebAuthnPrompt);
        this.mockNavigationService.Verify(n => n.NavigateTo("/"), Times.Once);
    }

    // --- ShowForgotPassword / CancelForgotPassword ---

    [Fact]
    public void GivenActive_WhenShowForgotPassword_ThenForgotPasswordModeIsEnabled()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.ShowForgotPasswordCommand.Execute(null);

        // Assert
        Assert.True(sut.ForgotPasswordMode);
        Assert.False(sut.ForgotPasswordCodeSent);
        Assert.Empty(sut.ForgotPasswordUsername);
        Assert.Null(sut.ErrorMessage);
    }

    [Fact]
    public void GivenForgotPasswordMode_WhenCancelForgotPassword_ThenForgotPasswordModeIsDisabled()
    {
        // Arrange
        var sut = CreateSut();
        sut.ForgotPasswordMode = true;
        sut.ForgotPasswordUsername = "test";
        sut.VerificationCode = "123456";

        // Act
        sut.CancelForgotPasswordCommand.Execute(null);

        // Assert
        Assert.False(sut.ForgotPasswordMode);
        Assert.Empty(sut.ForgotPasswordUsername);
        Assert.Empty(sut.VerificationCode);
    }

    // --- HandleForgotPasswordAsync ---

    [Fact]
    public async Task GivenEmptyUsername_WhenHandleForgotPassword_ThenErrorIsSet()
    {
        // Arrange
        var sut = CreateSut();
        sut.ForgotPasswordUsername = string.Empty;

        // Act
        await sut.HandleForgotPasswordCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal("Please enter your username or email address", sut.ErrorMessage);
    }

    [Fact]
    public async Task GivenValidUsername_WhenHandleForgotPassword_ThenCodeIsSent()
    {
        // Arrange
        var sut = CreateSut();
        sut.ForgotPasswordUsername = "testuser";
        this.mockAuthService.Setup(s => s.ForgotPassword("testuser"))
            .ReturnsAsync(new ForgotPasswordResult { Success = true });

        // Act
        await sut.HandleForgotPasswordCommand.ExecuteAsync(null);

        // Assert
        Assert.True(sut.ForgotPasswordCodeSent);
        Assert.Null(sut.ErrorMessage);
    }

    [Fact]
    public async Task GivenForgotPasswordFails_WhenHandleForgotPassword_ThenErrorIsSet()
    {
        // Arrange
        var sut = CreateSut();
        sut.ForgotPasswordUsername = "testuser";
        this.mockAuthService.Setup(s => s.ForgotPassword("testuser"))
            .ReturnsAsync(new ForgotPasswordResult { Success = false, Error = "User not found" });

        // Act
        await sut.HandleForgotPasswordCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal("User not found", sut.ErrorMessage);
        Assert.False(sut.ForgotPasswordCodeSent);
    }

    [Fact]
    public async Task GivenForgotPasswordThrows_WhenHandleForgotPassword_ThenErrorIsSet()
    {
        // Arrange
        var sut = CreateSut();
        sut.ForgotPasswordUsername = "testuser";
        this.mockAuthService.Setup(s => s.ForgotPassword("testuser"))
            .ThrowsAsync(new Exception("network error"));

        // Act
        await sut.HandleForgotPasswordCommand.ExecuteAsync(null);

        // Assert
        Assert.NotNull(sut.ErrorMessage);
        Assert.False(sut.IsLoading);
    }

    // --- HandleConfirmForgotPasswordAsync ---

    [Fact]
    public async Task GivenEmptyVerificationCode_WhenHandleConfirmForgotPassword_ThenErrorIsSet()
    {
        // Arrange
        var sut = CreateSut();
        sut.VerificationCode = string.Empty;

        // Act
        await sut.HandleConfirmForgotPasswordCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal("Please enter the verification code", sut.ErrorMessage);
    }

    [Fact]
    public async Task GivenEmptyNewPassword_WhenHandleConfirmForgotPassword_ThenErrorIsSet()
    {
        // Arrange
        var sut = CreateSut();
        sut.VerificationCode = "123456";
        sut.NewPassword = string.Empty;

        // Act
        await sut.HandleConfirmForgotPasswordCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal("Please enter a new password", sut.ErrorMessage);
    }

    [Fact]
    public async Task GivenMismatchedPasswords_WhenHandleConfirmForgotPassword_ThenErrorIsSet()
    {
        // Arrange
        var sut = CreateSut();
        sut.VerificationCode = "123456";
        sut.NewPassword = "pass1";
        sut.ConfirmPassword = "pass2";

        // Act
        await sut.HandleConfirmForgotPasswordCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal("Passwords do not match", sut.ErrorMessage);
    }

    [Fact]
    public async Task GivenValidInputAndSuccessfulReset_WhenHandleConfirmForgotPassword_ThenForgotPasswordModeIsFalse()
    {
        // Arrange
        var sut = CreateSut();
        sut.ForgotPasswordUsername = "user";
        sut.VerificationCode = "123456";
        sut.NewPassword = "newpass";
        sut.ConfirmPassword = "newpass";
        this.mockAuthService
            .Setup(s => s.ConfirmForgotPassword("user", "123456", "newpass"))
            .ReturnsAsync(new ForgotPasswordResult { Success = true });

        // Act
        await sut.HandleConfirmForgotPasswordCommand.ExecuteAsync(null);

        // Assert
        Assert.False(sut.ForgotPasswordMode);
        Assert.Equal("user", sut.Username);
        Assert.False(sut.IsLoading);
    }

    [Fact]
    public async Task GivenValidInputAndFailedReset_WhenHandleConfirmForgotPassword_ThenErrorIsSet()
    {
        // Arrange
        var sut = CreateSut();
        sut.ForgotPasswordUsername = "user";
        sut.VerificationCode = "123456";
        sut.NewPassword = "newpass";
        sut.ConfirmPassword = "newpass";
        this.mockAuthService
            .Setup(s => s.ConfirmForgotPassword(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ForgotPasswordResult { Success = false, Error = "Invalid code" });

        // Act
        await sut.HandleConfirmForgotPasswordCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal("Invalid code", sut.ErrorMessage);
        Assert.False(sut.IsLoading);
    }

    [Fact]
    public async Task GivenConfirmForgotPasswordThrows_WhenHandleConfirmForgotPassword_ThenErrorIsSet()
    {
        // Arrange
        var sut = CreateSut();
        sut.ForgotPasswordUsername = "user";
        sut.VerificationCode = "123456";
        sut.NewPassword = "newpass";
        sut.ConfirmPassword = "newpass";
        this.mockAuthService
            .Setup(s => s.ConfirmForgotPassword(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("confirm error"));

        // Act
        await sut.HandleConfirmForgotPasswordCommand.ExecuteAsync(null);

        // Assert
        Assert.NotNull(sut.ErrorMessage);
        Assert.False(sut.IsLoading);
    }
}
