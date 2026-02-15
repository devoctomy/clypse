using clypse.portal.Models.Aws;
using clypse.portal.Models.Login;

namespace clypse.portal.Application.Services.Interfaces;

/// <summary>
/// Provides authentication functionality for the application.
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Initializes the authentication service.
    /// </summary>
    /// <returns>Nothing.</returns>
    Task Initialize();

    /// <summary>
    /// Checks if the user is currently authenticated.
    /// </summary>
    /// <returns>True if authenticated, false otherwise.</returns>
    Task<bool> CheckAuthentication();

    /// <summary>
    /// Attempts to log in a user with the provided credentials.
    /// </summary>
    /// <param name="username">The username to authenticate with.</param>
    /// <param name="password">The password to authenticate with.</param>
    /// <returns>A LoginResult containing the authentication status and any associated credentials.</returns>
    Task<LoginResult> Login(string username, string password);

    /// <summary>
    /// Logs out the current user and clears any stored credentials.
    /// </summary>
    /// <returns>Nothing.</returns>
    Task Logout();

    /// <summary>
    /// Retrieves any stored credentials for the current session.
    /// </summary>
    /// <returns>The stored credentials if available, null otherwise.</returns>
    Task<StoredCredentials?> GetStoredCredentials();

    /// <summary>
    /// Completes the password reset process with a new password.
    /// </summary>
    /// <param name="username">The username for which to reset the password.</param>
    /// <param name="newPassword">The new password to set.</param>
    /// <returns>A LoginResult containing the authentication status and any associated credentials.</returns>
    Task<LoginResult> CompletePasswordReset(string username, string newPassword);

    /// <summary>
    /// Initiates a forgot password flow by sending a verification code to the user's registered email/phone.
    /// </summary>
    /// <param name="username">The username for which to initiate password reset.</param>
    /// <returns>A ForgotPasswordResult containing the operation status and delivery details.</returns>
    Task<ForgotPasswordResult> ForgotPassword(string username);

    /// <summary>
    /// Confirms the forgot password flow by verifying the code and setting a new password.
    /// </summary>
    /// <param name="username">The username for which to confirm password reset.</param>
    /// <param name="verificationCode">The verification code sent to the user.</param>
    /// <param name="newPassword">The new password to set.</param>
    /// <returns>A ForgotPasswordResult containing the operation status.</returns>
    Task<ForgotPasswordResult> ConfirmForgotPassword(string username, string verificationCode, string newPassword);
}
