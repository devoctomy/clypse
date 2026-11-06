namespace Clypse.Core.Password;

/// <summary>
/// Checks passwords against a list of weak known passwords.
/// </summary>
public interface IWeakKnownPasswordsService
{
    /// <summary>
    /// Checks whether a password is in the list of weak known passwords.
    /// </summary>
    /// <param name="password">The password to check.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>True if the password is a weak known password.</returns>
    public Task<bool> ContainsAsync(string password, CancellationToken cancellationToken);
}
