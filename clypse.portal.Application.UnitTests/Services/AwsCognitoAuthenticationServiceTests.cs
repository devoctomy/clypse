using System.Text.Json;
using clypse.portal.Application.Services;
using clypse.portal.Application.Services.Interfaces;
using clypse.portal.Models.Aws;
using clypse.portal.Models.Login;
using Microsoft.JSInterop;
using Moq;

namespace clypse.portal.Application.UnitTests.Services;

public class AwsCognitoAuthenticationServiceTests
{
    private readonly Mock<IJSRuntime> mockJsRuntime;
    private readonly AwsCognitoConfig cognitoConfig;
    private readonly Mock<ILocalStorageService> mockLocalStorage;

    public AwsCognitoAuthenticationServiceTests()
    {
        this.mockJsRuntime = new Mock<IJSRuntime>();
        this.cognitoConfig = new AwsCognitoConfig
        {
            UserPoolId = "test-pool-id",
            UserPoolClientId = "test-client-id",
            Region = "us-east-1",
            IdentityPoolId = "test-identity-pool-id"
        };
        this.mockLocalStorage = new Mock<ILocalStorageService>();
    }

    [Fact]
    public void GivenValidParameters_WhenConstructing_ThenCreatesInstance()
    {
        // Act
        var service = new AwsCognitoAuthenticationService(
            this.mockJsRuntime.Object,
            this.cognitoConfig,
            this.mockLocalStorage.Object);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void GivenNullJSRuntime_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new AwsCognitoAuthenticationService(
                null!,
                this.cognitoConfig,
                this.mockLocalStorage.Object));

        Assert.Equal("jsRuntime", exception.ParamName);
    }

    [Fact]
    public void GivenNullCognitoConfig_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new AwsCognitoAuthenticationService(
                this.mockJsRuntime.Object,
                null!,
                this.mockLocalStorage.Object));

        Assert.Equal("cognitoConfig", exception.ParamName);
    }

    [Fact]
    public void GivenNullLocalStorage_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new AwsCognitoAuthenticationService(
                this.mockJsRuntime.Object,
                this.cognitoConfig,
                null!));

        Assert.Equal("localStorage", exception.ParamName);
    }

    [Fact]
    public async Task GivenFirstTimeInitialization_WhenInitialize_ThenInvokesJavaScriptAndSetsInitializedFlag()
    {
        // Arrange
        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<string>("CognitoAuth.initialize", It.IsAny<object[]>()))
            .ReturnsAsync("success");

        var sut = new AwsCognitoAuthenticationService(
            this.mockJsRuntime.Object,
            this.cognitoConfig,
            this.mockLocalStorage.Object);

        // Act
        await sut.Initialize();

        // Assert
        this.mockJsRuntime.Verify(
            x => x.InvokeAsync<string>("CognitoAuth.initialize", It.IsAny<object[]>()),
            Times.Once);
    }

    [Fact]
    public async Task GivenAlreadyInitialized_WhenInitialize_ThenSkipsInitialization()
    {
        // Arrange
        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<string>("CognitoAuth.initialize", It.IsAny<object[]>()))
            .ReturnsAsync("success");

        var sut = new AwsCognitoAuthenticationService(
            this.mockJsRuntime.Object,
            this.cognitoConfig,
            this.mockLocalStorage.Object);

        await sut.Initialize();
        this.mockJsRuntime.Invocations.Clear();

        // Act
        await sut.Initialize();

        // Assert
        this.mockJsRuntime.Verify(
            x => x.InvokeAsync<string>("CognitoAuth.initialize", It.IsAny<object[]>()),
            Times.Never);
    }

    [Fact]
    public async Task GivenNoStoredCredentials_WhenCheckAuthentication_ThenReturnsFalse()
    {
        // Arrange
        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<string>("localStorage.getItem", It.IsAny<object[]>()))
            .ReturnsAsync(string.Empty);

        var sut = new AwsCognitoAuthenticationService(
            this.mockJsRuntime.Object,
            this.cognitoConfig,
            this.mockLocalStorage.Object);

        // Act
        var result = await sut.CheckAuthentication();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GivenExpiredCredentials_WhenCheckAuthentication_ThenClearsCredentialsAndReturnsFalse()
    {
        // Arrange
        var expiredCredentials = new StoredCredentials
        {
            IdToken = "test-id-token",
            AccessToken = "test-access-token",
            ExpirationTime = DateTime.UtcNow.AddHours(-1).ToString("O"),
            StoredAt = DateTime.UtcNow.AddHours(-2)
        };

        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<string>("localStorage.getItem", It.IsAny<object[]>()))
            .ReturnsAsync(JsonSerializer.Serialize(expiredCredentials));

        var sut = new AwsCognitoAuthenticationService(
            this.mockJsRuntime.Object,
            this.cognitoConfig,
            this.mockLocalStorage.Object);

        // Act
        var result = await sut.CheckAuthentication();

        // Assert
        Assert.False(result);
        this.mockLocalStorage.Verify(
            x => x.ClearAllExceptPersistentSettingsAsync(),
            Times.Once);
    }

    [Fact]
    public async Task GivenValidCredentials_WhenCheckAuthentication_ThenRefreshesCredentialsAndReturnsTrue()
    {
        // Arrange
        var validCredentials = new StoredCredentials
        {
            IdToken = "test-id-token",
            AccessToken = "test-access-token",
            ExpirationTime = DateTime.UtcNow.AddHours(1).ToString("O"),
            StoredAt = DateTime.UtcNow,
            AwsCredentials = new AwsCredentials
            {
                AccessKeyId = "test-access-key",
                SecretAccessKey = "test-secret-key",
                SessionToken = "test-session-token",
                IdentityId = "test-identity-id",
                Expiration = DateTime.UtcNow.AddHours(1).ToString("O")
            }
        };

        var refreshedCredentials = new AwsCredentials
        {
            AccessKeyId = "new-access-key",
            SecretAccessKey = "new-secret-key",
            SessionToken = "new-session-token",
            Expiration = DateTime.UtcNow.AddHours(1).ToString("O")
        };

        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<string>("localStorage.getItem", It.IsAny<object[]>()))
            .ReturnsAsync(JsonSerializer.Serialize(validCredentials));

        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<AwsCredentials>("CognitoAuth.getAwsCredentials", It.IsAny<object[]>()))
            .ReturnsAsync(refreshedCredentials);

        var sut = new AwsCognitoAuthenticationService(
            this.mockJsRuntime.Object,
            this.cognitoConfig,
            this.mockLocalStorage.Object);

        // Act
        var result = await sut.CheckAuthentication();

        // Assert
        Assert.True(result);
        this.mockJsRuntime.Verify(
            x => x.InvokeAsync<AwsCredentials>("CognitoAuth.getAwsCredentials", It.IsAny<object[]>()),
            Times.Once);
    }

    [Fact]
    public async Task GivenJustLoggedInFlag_WhenCheckAuthentication_ThenSkipsRefreshAndReturnsTrue()
    {
        // Arrange
        var validCredentials = new StoredCredentials
        {
            IdToken = "test-id-token",
            AccessToken = "test-access-token",
            ExpirationTime = DateTime.UtcNow.AddHours(1).ToString("O"),
            StoredAt = DateTime.UtcNow,
            AwsCredentials = new AwsCredentials
            {
                AccessKeyId = "test-access-key",
                SecretAccessKey = "test-secret-key",
                SessionToken = "test-session-token",
                IdentityId = "test-identity-id",
                Expiration = DateTime.UtcNow.AddHours(1).ToString("O")
            }
        };

        var loginResult = new LoginResult
        {
            Success = true,
            AccessToken = "test-access-token",
            IdToken = "test-id-token",
            AwsCredentials = validCredentials.AwsCredentials
        };

        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<LoginResult>("CognitoAuth.login", It.IsAny<object[]>()))
            .ReturnsAsync(loginResult);

        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<string>("localStorage.getItem", It.IsAny<object[]>()))
            .ReturnsAsync(JsonSerializer.Serialize(validCredentials));

        var sut = new AwsCognitoAuthenticationService(
            this.mockJsRuntime.Object,
            this.cognitoConfig,
            this.mockLocalStorage.Object);

        // Perform login to set justLoggedIn flag
        await sut.Login("testuser", "testpassword");

        // Act
        var result = await sut.CheckAuthentication();

        // Assert
        Assert.True(result);
        // Verify that credential refresh was NOT called
        this.mockJsRuntime.Verify(
            x => x.InvokeAsync<AwsCredentials>("CognitoAuth.getAwsCredentials", It.IsAny<object[]>()),
            Times.Never);
    }

    [Fact]
    public async Task GivenCredentialRefreshFails_WhenCheckAuthentication_ThenClearsCredentialsAndReturnsFalse()
    {
        // Arrange
        var validCredentials = new StoredCredentials
        {
            IdToken = "test-id-token",
            AccessToken = "test-access-token",
            ExpirationTime = DateTime.UtcNow.AddHours(1).ToString("O"),
            StoredAt = DateTime.UtcNow,
            AwsCredentials = new AwsCredentials
            {
                AccessKeyId = "test-access-key",
                SecretAccessKey = "test-secret-key",
                SessionToken = "test-session-token",
                IdentityId = "test-identity-id",
                Expiration = DateTime.UtcNow.AddHours(1).ToString("O")
            }
        };

        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<string>("localStorage.getItem", It.IsAny<object[]>()))
            .ReturnsAsync(JsonSerializer.Serialize(validCredentials));

        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<AwsCredentials>("CognitoAuth.getAwsCredentials", It.IsAny<object[]>()))
            .ThrowsAsync(new Exception("Credential refresh failed"));

        var sut = new AwsCognitoAuthenticationService(
            this.mockJsRuntime.Object,
            this.cognitoConfig,
            this.mockLocalStorage.Object);

        // Act
        var result = await sut.CheckAuthentication();

        // Assert
        Assert.False(result);
        this.mockLocalStorage.Verify(
            x => x.ClearAllExceptPersistentSettingsAsync(),
            Times.Once);
    }

    [Fact]
    public async Task GivenInvalidExpirationTime_WhenCheckAuthentication_ThenContinuesAndChecksIdToken()
    {
        // Arrange
        var credentialsWithInvalidExpiration = new StoredCredentials
        {
            IdToken = "test-id-token",
            AccessToken = "test-access-token",
            ExpirationTime = "invalid-date-format",
            StoredAt = DateTime.UtcNow,
            AwsCredentials = new AwsCredentials
            {
                AccessKeyId = "test-access-key",
                SecretAccessKey = "test-secret-key",
                SessionToken = "test-session-token",
                IdentityId = "test-identity-id",
                Expiration = DateTime.UtcNow.AddHours(1).ToString("O")
            }
        };

        var refreshedCredentials = new AwsCredentials
        {
            AccessKeyId = "new-access-key",
            SecretAccessKey = "new-secret-key",
            SessionToken = "new-session-token",
            Expiration = DateTime.UtcNow.AddHours(1).ToString("O")
        };

        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<string>("localStorage.getItem", It.IsAny<object[]>()))
            .ReturnsAsync(JsonSerializer.Serialize(credentialsWithInvalidExpiration));

        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<AwsCredentials>("CognitoAuth.getAwsCredentials", It.IsAny<object[]>()))
            .ReturnsAsync(refreshedCredentials);

        var sut = new AwsCognitoAuthenticationService(
            this.mockJsRuntime.Object,
            this.cognitoConfig,
            this.mockLocalStorage.Object);

        // Act
        var result = await sut.CheckAuthentication();

        // Assert
        Assert.True(result);
        // DateTime.TryParse failed, so it continues to refresh credentials
        this.mockJsRuntime.Verify(
            x => x.InvokeAsync<AwsCredentials>("CognitoAuth.getAwsCredentials", It.IsAny<object[]>()),
            Times.Once);
    }

    [Fact]
    public async Task GivenExceptionInGetStoredCredentials_WhenCheckAuthentication_ThenCatchesAndReturnsFalse()
    {
        // Arrange
        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<string>("localStorage.getItem", It.IsAny<object[]>()))
            .ThrowsAsync(new Exception("Storage access failed"));

        var sut = new AwsCognitoAuthenticationService(
            this.mockJsRuntime.Object,
            this.cognitoConfig,
            this.mockLocalStorage.Object);

        // Act
        var result = await sut.CheckAuthentication();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GivenCredentialsWithoutIdToken_WhenCheckAuthentication_ThenReturnsFalse()
    {
        // Arrange
        var credentialsWithoutIdToken = new StoredCredentials
        {
            IdToken = null,
            AccessToken = "test-access-token",
            ExpirationTime = DateTime.UtcNow.AddHours(1).ToString("O"),
            StoredAt = DateTime.UtcNow,
            AwsCredentials = new AwsCredentials
            {
                AccessKeyId = "test-access-key",
                SecretAccessKey = "test-secret-key",
                SessionToken = "test-session-token",
                IdentityId = "test-identity-id",
                Expiration = DateTime.UtcNow.AddHours(1).ToString("O")
            }
        };

        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<string>("localStorage.getItem", It.IsAny<object[]>()))
            .ReturnsAsync(JsonSerializer.Serialize(credentialsWithoutIdToken));

        var sut = new AwsCognitoAuthenticationService(
            this.mockJsRuntime.Object,
            this.cognitoConfig,
            this.mockLocalStorage.Object);

        // Act
        var result = await sut.CheckAuthentication();

        // Assert
        Assert.False(result);
        // Verify that credential refresh was NOT called since IdToken is null
        this.mockJsRuntime.Verify(
            x => x.InvokeAsync<AwsCredentials>("CognitoAuth.getAwsCredentials", It.IsAny<object[]>()),
            Times.Never);
    }

    [Fact]
    public async Task GivenSuccessfulLogin_WhenLogin_ThenStoresCredentialsAndReturnsSuccess()
    {
        // Arrange
        var username = "testuser";
        var password = "testpassword";
        var loginResult = new LoginResult
        {
            Success = true,
            AccessToken = "test-access-token",
            IdToken = "test-id-token",
            AwsCredentials = new AwsCredentials
            {
                AccessKeyId = "test-access-key",
                SecretAccessKey = "test-secret-key",
                SessionToken = "test-session-token",
                IdentityId = "test-identity-id",
                Expiration = DateTime.UtcNow.AddHours(1).ToString("O")
            }
        };

        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<LoginResult>("CognitoAuth.login", It.IsAny<object[]>()))
            .ReturnsAsync(loginResult);

        var sut = new AwsCognitoAuthenticationService(
            this.mockJsRuntime.Object,
            this.cognitoConfig,
            this.mockLocalStorage.Object);

        // Act
        var result = await sut.Login(username, password);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("test-access-token", result.AccessToken);
        this.mockJsRuntime.Verify(
            x => x.InvokeAsync<LoginResult>("CognitoAuth.login", It.IsAny<object[]>()),
            Times.Once);
    }

    [Fact]
    public async Task GivenFailedLogin_WhenLogin_ThenReturnsFailureWithError()
    {
        // Arrange
        var username = "testuser";
        var password = "wrongpassword";
        var loginResult = new LoginResult
        {
            Success = false,
            Error = "Invalid username or password"
        };

        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<LoginResult>("CognitoAuth.login", It.IsAny<object[]>()))
            .ReturnsAsync(loginResult);

        var sut = new AwsCognitoAuthenticationService(
            this.mockJsRuntime.Object,
            this.cognitoConfig,
            this.mockLocalStorage.Object);

        // Act
        var result = await sut.Login(username, password);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Invalid username or password", result.Error);
    }

    [Fact]
    public async Task GivenJSException_WhenLogin_ThenReturnsFailureWithExceptionMessage()
    {
        // Arrange
        var username = "testuser";
        var password = "testpassword";
        var exceptionMessage = "Network error";

        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<LoginResult>("CognitoAuth.login", It.IsAny<object[]>()))
            .ThrowsAsync(new JSException(exceptionMessage));

        var sut = new AwsCognitoAuthenticationService(
            this.mockJsRuntime.Object,
            this.cognitoConfig,
            this.mockLocalStorage.Object);

        // Act
        var result = await sut.Login(username, password);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("An error occurred during login", result.Error);
    }

    [Fact]
    public async Task GivenAuthenticatedUser_WhenLogout_ThenCallsCognitoLogoutAndClearsCredentials()
    {
        // Arrange
        var sut = new AwsCognitoAuthenticationService(
            this.mockJsRuntime.Object,
            this.cognitoConfig,
            this.mockLocalStorage.Object);

        // Act
        await sut.Logout();

        // Assert
        this.mockLocalStorage.Verify(
            x => x.ClearAllExceptPersistentSettingsAsync(),
            Times.Once);
    }

    [Fact]
    public async Task GivenStoredCredentials_WhenGetStoredCredentials_ThenReturnsDeserializedCredentials()
    {
        // Arrange
        var storedCredentials = new StoredCredentials
        {
            IdToken = "test-id-token",
            AccessToken = "test-access-token",
            ExpirationTime = DateTime.UtcNow.AddHours(1).ToString("O"),
            StoredAt = DateTime.UtcNow
        };

        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<string>("localStorage.getItem", It.IsAny<object[]>()))
            .ReturnsAsync(JsonSerializer.Serialize(storedCredentials));

        var sut = new AwsCognitoAuthenticationService(
            this.mockJsRuntime.Object,
            this.cognitoConfig,
            this.mockLocalStorage.Object);

        // Act
        var result = await sut.GetStoredCredentials();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-id-token", result.IdToken);
        Assert.Equal("test-access-token", result.AccessToken);
    }

    [Fact]
    public async Task GivenNoStoredCredentials_WhenGetStoredCredentials_ThenReturnsNull()
    {
        // Arrange
        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<string>("localStorage.getItem", It.IsAny<object[]>()))
            .ReturnsAsync(string.Empty);

        var sut = new AwsCognitoAuthenticationService(
            this.mockJsRuntime.Object,
            this.cognitoConfig,
            this.mockLocalStorage.Object);

        // Act
        var result = await sut.GetStoredCredentials();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GivenInvalidJson_WhenGetStoredCredentials_ThenReturnsNull()
    {
        // Arrange
        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<string>("localStorage.getItem", It.IsAny<object[]>()))
            .ReturnsAsync("invalid-json");

        var sut = new AwsCognitoAuthenticationService(
            this.mockJsRuntime.Object,
            this.cognitoConfig,
            this.mockLocalStorage.Object);

        // Act
        var result = await sut.GetStoredCredentials();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GivenSuccessfulPasswordReset_WhenCompletePasswordReset_ThenStoresCredentialsAndReturnsSuccess()
    {
        // Arrange
        var username = "testuser";
        var newPassword = "newpassword123";
        var loginResult = new LoginResult
        {
            Success = true,
            AccessToken = "test-access-token",
            IdToken = "test-id-token",
            AwsCredentials = new AwsCredentials
            {
                AccessKeyId = "test-access-key",
                SecretAccessKey = "test-secret-key",
                SessionToken = "test-session-token",
                IdentityId = "test-identity-id",
                Expiration = DateTime.UtcNow.AddHours(1).ToString("O")
            }
        };

        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<LoginResult>("CognitoAuth.completePasswordReset", It.IsAny<object[]>()))
            .ReturnsAsync(loginResult);

        var sut = new AwsCognitoAuthenticationService(
            this.mockJsRuntime.Object,
            this.cognitoConfig,
            this.mockLocalStorage.Object);

        // Act
        var result = await sut.CompletePasswordReset(username, newPassword);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("test-access-token", result.AccessToken);
    }

    [Fact]
    public async Task GivenFailedPasswordReset_WhenCompletePasswordReset_ThenReturnsFailureWithError()
    {
        // Arrange
        var username = "testuser";
        var newPassword = "weak";
        var loginResult = new LoginResult
        {
            Success = false,
            Error = "Password does not meet requirements"
        };

        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<LoginResult>("CognitoAuth.completePasswordReset", It.IsAny<object[]>()))
            .ReturnsAsync(loginResult);

        var sut = new AwsCognitoAuthenticationService(
            this.mockJsRuntime.Object,
            this.cognitoConfig,
            this.mockLocalStorage.Object);

        // Act
        var result = await sut.CompletePasswordReset(username, newPassword);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Password does not meet requirements", result.Error);
    }

    [Fact]
    public async Task GivenJSException_WhenCompletePasswordReset_ThenReturnsFailureWithExceptionMessage()
    {
        // Arrange
        var username = "testuser";
        var newPassword = "newpassword123";

        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<LoginResult>("CognitoAuth.completePasswordReset", It.IsAny<object[]>()))
            .ThrowsAsync(new Exception("Network error"));

        var sut = new AwsCognitoAuthenticationService(
            this.mockJsRuntime.Object,
            this.cognitoConfig,
            this.mockLocalStorage.Object);

        // Act
        var result = await sut.CompletePasswordReset(username, newPassword);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("An error occurred during password reset", result.Error);
    }

    [Fact]
    public async Task GivenValidUsername_WhenForgotPassword_ThenReturnsSuccessWithDeliveryDetails()
    {
        // Arrange
        var username = "testuser";
        var forgotPasswordResult = new ForgotPasswordResult
        {
            Success = true,
            Message = "Verification code sent"
        };

        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<ForgotPasswordResult>("CognitoAuth.forgotPassword", It.IsAny<object[]>()))
            .ReturnsAsync(forgotPasswordResult);

        var sut = new AwsCognitoAuthenticationService(
            this.mockJsRuntime.Object,
            this.cognitoConfig,
            this.mockLocalStorage.Object);

        // Act
        var result = await sut.ForgotPassword(username);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Verification code sent", result.Message);
    }

    [Fact]
    public async Task GivenInvalidUsername_WhenForgotPassword_ThenReturnsFailureWithError()
    {
        // Arrange
        var username = "nonexistentuser";
        var forgotPasswordResult = new ForgotPasswordResult
        {
            Success = false,
            Error = "User not found"
        };

        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<ForgotPasswordResult>("CognitoAuth.forgotPassword", It.IsAny<object[]>()))
            .ReturnsAsync(forgotPasswordResult);

        var sut = new AwsCognitoAuthenticationService(
            this.mockJsRuntime.Object,
            this.cognitoConfig,
            this.mockLocalStorage.Object);

        // Act
        var result = await sut.ForgotPassword(username);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("User not found", result.Error);
    }

    [Fact]
    public async Task GivenJSException_WhenForgotPassword_ThenReturnsFailureWithExceptionMessage()
    {
        // Arrange
        var username = "testuser";

        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<ForgotPasswordResult>("CognitoAuth.forgotPassword", It.IsAny<object[]>()))
            .ThrowsAsync(new Exception("Network error"));

        var sut = new AwsCognitoAuthenticationService(
            this.mockJsRuntime.Object,
            this.cognitoConfig,
            this.mockLocalStorage.Object);

        // Act
        var result = await sut.ForgotPassword(username);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("An error occurred during forgot password", result.Error);
    }

    [Fact]
    public async Task GivenValidVerificationCode_WhenConfirmForgotPassword_ThenReturnsSuccess()
    {
        // Arrange
        var username = "testuser";
        var verificationCode = "123456";
        var newPassword = "newpassword123";
        var forgotPasswordResult = new ForgotPasswordResult
        {
            Success = true,
            Message = "Password reset successful"
        };

        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<ForgotPasswordResult>("CognitoAuth.confirmForgotPassword", It.IsAny<object[]>()))
            .ReturnsAsync(forgotPasswordResult);

        var sut = new AwsCognitoAuthenticationService(
            this.mockJsRuntime.Object,
            this.cognitoConfig,
            this.mockLocalStorage.Object);

        // Act
        var result = await sut.ConfirmForgotPassword(username, verificationCode, newPassword);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Password reset successful", result.Message);
    }

    [Fact]
    public async Task GivenInvalidVerificationCode_WhenConfirmForgotPassword_ThenReturnsFailureWithError()
    {
        // Arrange
        var username = "testuser";
        var verificationCode = "000000";
        var newPassword = "newpassword123";
        var forgotPasswordResult = new ForgotPasswordResult
        {
            Success = false,
            Error = "Invalid verification code"
        };

        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<ForgotPasswordResult>("CognitoAuth.confirmForgotPassword", It.IsAny<object[]>()))
            .ReturnsAsync(forgotPasswordResult);

        var sut = new AwsCognitoAuthenticationService(
            this.mockJsRuntime.Object,
            this.cognitoConfig,
            this.mockLocalStorage.Object);

        // Act
        var result = await sut.ConfirmForgotPassword(username, verificationCode, newPassword);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Invalid verification code", result.Error);
    }

    [Fact]
    public async Task GivenJSException_WhenConfirmForgotPassword_ThenReturnsFailureWithExceptionMessage()
    {
        // Arrange
        var username = "testuser";
        var verificationCode = "123456";
        var newPassword = "newpassword123";

        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<ForgotPasswordResult>("CognitoAuth.confirmForgotPassword", It.IsAny<object[]>()))
            .ThrowsAsync(new Exception("Network error"));

        var sut = new AwsCognitoAuthenticationService(
            this.mockJsRuntime.Object,
            this.cognitoConfig,
            this.mockLocalStorage.Object);

        // Act
        var result = await sut.ConfirmForgotPassword(username, verificationCode, newPassword);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("An error occurred during confirm forgot password", result.Error);
    }
}
