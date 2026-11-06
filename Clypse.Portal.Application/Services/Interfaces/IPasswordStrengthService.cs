using Clypse.Portal.Models.Vaults;

namespace Clypse.Portal.Application.Services.Interfaces;

/// <summary>
/// Estimates how strong passwords are, and words it.
/// </summary>
public interface IPasswordStrengthService
{
    /// <summary>
    /// Estimates how strong a password is, from its length and the kinds of character in it.
    /// </summary>
    /// <param name="password">The password.</param>
    /// <param name="ct">A token to cancel with.</param>
    /// <returns>The strength.</returns>
    Task<PasswordStrength> EstimateAsync(string? password, CancellationToken ct = default);

    /// <summary>
    /// Words a strength, e.g. "Strong, about 98 bits".
    /// </summary>
    /// <param name="strength">The strength.</param>
    /// <returns>The text, or an empty string for no password.</returns>
    string Describe(PasswordStrength strength);
}
