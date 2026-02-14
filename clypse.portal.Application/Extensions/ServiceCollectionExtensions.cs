using clypse.portal.Application.Services;
using clypse.portal.Application.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace clypse.portal.Application.Extensions;

public static class ServiceCollectionExtensions
{
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
