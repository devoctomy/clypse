using clypse.portal.Models;

namespace clypse.portal.Services;

/// <summary>
/// Provides authentication functionality for the application.
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Initializes the authentication service.
    /// </summary>
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
    Task Logout();

    /// <summary>
    /// Retrieves any stored credentials for the current session.
    /// </summary>
    /// <returns>The stored credentials if available, null otherwise.</returns>
    Task<StoredCredentials?> GetStoredCredentials();
}
