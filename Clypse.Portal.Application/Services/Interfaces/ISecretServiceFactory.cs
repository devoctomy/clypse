using Clypse.Portal.Models.Vaults;

namespace Clypse.Portal.Application.Services.Interfaces;

/// <summary>
/// Creates the <see cref="ISecretService"/> of a vault.
/// </summary>
public interface ISecretServiceFactory
{
    /// <summary>
    /// Creates the secret service of a vault.
    /// </summary>
    /// <param name="vault">The vault.</param>
    /// <returns>Its secrets.</returns>
    ISecretService Create(Vault vault);
}
