using Clypse.Portal.Application.Services.Interfaces;
using Clypse.Portal.Models.Vaults;
using Devoctomy.Blazwiz.Sync.Interfaces;

namespace Clypse.Portal.Application.Services;

/// <summary>
/// Implementation of <see cref="ISecretServiceFactory"/>: a <see cref="SecretService"/> over the vault's sync profile's
/// file store, which encrypts and decrypts with the vault's own keys.
/// </summary>
/// <param name="registry">The sync profiles, for each vault's file store.</param>
/// <param name="timeProvider">The clock.</param>
public class SecretServiceFactory(ISyncProfileRegistry registry, TimeProvider timeProvider)
    : ISecretServiceFactory
{
    private readonly ISyncProfileRegistry registry = registry ?? throw new ArgumentNullException(nameof(registry));
    private readonly TimeProvider timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    /// <inheritdoc/>
    public ISecretService Create(Vault vault)
    {
        ArgumentNullException.ThrowIfNull(vault);

        return new SecretService(vault, registry.GetFileStore(vault.Id), timeProvider);
    }
}
