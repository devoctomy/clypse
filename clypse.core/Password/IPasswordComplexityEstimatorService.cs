using clypse.core.Enums;

namespace clypse.core.Password;

/// <summary>
/// Interface for services that estimate the complexity of passwords.
/// </summary>
public interface IPasswordComplexityEstimatorService
{
    /// <summary>
    /// Estimates the entropy of the given password.
    /// </summary>
    /// <param name="password">The password to estimate.</param>
    /// <returns>A double value representing the estimated entropy.</returns>
    public double EstimateEntropy(string password);

    /// <summary>
    /// Estimates the complexity of the given password.
    /// </summary>
    /// <param name="password">The password to estimate.</param>
    /// <param name="checkForPwnedPasswords">Whether to check the password against a database of known compromised passwords.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="PasswordComplexityEstimatorResults"/> value representing the estimated complexity and any additional information.</returns>
    public Task<PasswordComplexityEstimatorResults> EstimateAsync(
        string password,
        bool checkForPwnedPasswords,
        CancellationToken cancellationToken);
}
