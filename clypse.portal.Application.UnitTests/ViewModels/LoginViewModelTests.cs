using clypse.portal.Application.Services.Interfaces;
using clypse.portal.Application.ViewModels;
using clypse.portal.Models.Login;
using clypse.portal.Models.Settings;

using clypse.core.Cryptography.Interfaces;
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
        // Act & Assert
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
        // Act & Assert
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

    [Fact]
    public void GivenNewInstance_WhenCheckingInitialState_ThenDefaultValuesAreCorrect()
    {
        // Arrange & Act
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

    [Fact]
    public async Task GivenCurrentThemeIsLight_WhenToggleTheme_ThenThemeChangesToDark()
    {
        // Arrange
        var sut = CreateSut();
        this.mockUserSettingsService.Setup(s => s.SetThemeAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        this.mockBrowserInteropService.Setup(s => s.SetThemeAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

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
        this.mockUserSettingsService.Setup(s => s.SetThemeAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        this.mockBrowserInteropService.Setup(s => s.SetThemeAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        // Act
        await sut.ToggleThemeCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal("light", sut.CurrentTheme);
        Assert.Equal("bi-moon", sut.ThemeIcon);
    }

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
}
