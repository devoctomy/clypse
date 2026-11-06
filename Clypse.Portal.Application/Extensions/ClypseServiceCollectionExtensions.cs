using Clypse.Portal.Application.Services;
using Clypse.Portal.Application.Services.Interfaces;
using Clypse.Portal.Application.Text;
using Devoctomy.Blazwiz.Auth.Extensions;
using Devoctomy.Blazwiz.UI.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Clypse.Portal.Application.Extensions;

/// <summary>
/// Extension methods for clypse's vaults and secrets.
/// </summary>
public static class ClypseServiceCollectionExtensions
{
    /// <summary>
    /// Adds clypse's vaults (each a protected sync profile the user creates) and their secrets, and the text of its
    /// errors. Needs the sync module and the clypse core services (<c>AddClypseCore</c>).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddClypse(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddErrorText<ClypseText>();
        services.AddScoped<IVaultService, VaultService>();
        services.AddScoped<ISecretServiceFactory, SecretServiceFactory>();
        services.AddScoped<IVaultSession, VaultSession>();
        services.AddSessionLifecycleHandler<VaultLabelHandler>();
        services.TryAddSingleton(TimeProvider.System);
        return services;
    }
}
