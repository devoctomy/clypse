using Clypse.Portal.Models.Vaults;

namespace Clypse.Portal.Application.Services.Interfaces;

/// <summary>
/// Creates the <see cref="IVaultFileService"/> of a vault.
/// </summary>
public interface IVaultFileServiceFactory
{
    /// <summary>
    /// Creates the file service of a vault.
    /// </summary>
    /// <param name="vault">The vault.</param>
    /// <returns>Its files.</returns>
    IVaultFileService Create(Vault vault);
}
