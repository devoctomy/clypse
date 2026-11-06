using Clypse.Portal.Application.Services;
using Clypse.Portal.Application.Services.Interfaces;
using Clypse.Portal.Application.Text;
using Clypse.Portal.Models.Settings;
using Devoctomy.Blazwiz.Auth.Extensions;
using Devoctomy.Blazwiz.UI.Extensions;
using Devoctomy.Blazwiz.UI.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Clypse.Portal.Application.Extensions;

/// <summary>
/// Extension methods for clypse's vaults and secrets.
/// </summary>
public static class ClypseServiceCollectionExtensions
{
    /// <summary>
    /// Adds clypse's vaults (each a protected sync profile the user creates), their secrets, the Vaults page in the
    /// sidebar, the text of clypse's errors, and its settings (the <see cref="ClypseSettings.SectionName"/> section).
    /// Needs the sync module and the clypse core services (<c>AddClypseCore</c>).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The app's configuration.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddClypse(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var settings = configuration.GetSection(ClypseSettings.SectionName).Get<ClypseSettings>() ?? new ClypseSettings();
        services.AddSingleton(settings);
        services.AddErrorText<ClypseText>();
        services.AddScoped<IVaultService, VaultService>();
        services.AddScoped<ISecretServiceFactory, SecretServiceFactory>();
        services.AddScoped<IVaultSession, VaultSession>();
        services.AddScoped<IPasswordStrengthService, PasswordStrengthService>();
        services.AddSessionLifecycleHandler<VaultLabelHandler>();
        services.TryAddSingleton(TimeProvider.System);
        services.AddNavigationItem(new NavigationItem
        {
            Text = "Nav.Vaults",
            Description = "Nav.Vaults.Description",
            TextResource = typeof(ClypseText),
            Href = "vaults",
            Icon = "bi-safe2",
            Order = 0,
        });
        return services;
    }
}
