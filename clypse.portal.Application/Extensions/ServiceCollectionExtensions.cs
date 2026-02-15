using clypse.portal.Application.Services;
using clypse.portal.Application.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace clypse.portal.Application.Extensions;

/// <summary>
/// Extension methods for configuring application logic services in an <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all application logic services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddApplicationLogicServices(this IServiceCollection services)
    {
        services.AddScoped<ILocalStorageService, LocalStorageService>();
        services.AddScoped<IVaultManagerFactoryService, VaultManagerFactoryService>();
        services.AddScoped<IVaultManagerBootstrapperFactoryService, VaultManagerBootstrapperFactoryService>();
        services.AddScoped<IVaultStorageService, VaultStorageService>();
        services.AddScoped<IAuthenticationService, AwsCognitoAuthenticationService>();
        services.AddScoped<IPwaUpdateService, PwaUpdateService>();
        services.AddScoped<IUserSettingsService, UserSettingsService>();

        return services;
    }
}
