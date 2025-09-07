using clypse.core.Cryptogtaphy;
using clypse.core.Cryptogtaphy.Interfaces;
using clypse.core.Password;
using Microsoft.Extensions.DependencyInjection;

namespace clypse.core.Extensions;

/// <summary>
/// Extension methods for IServiceCollection to add Clypse core services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds core Clypse services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddClypseCoreServices(this IServiceCollection services)
    {
        services.AddScoped<IRandomGeneratorService, RandomGeneratorService>();
        services.AddScoped<IPasswordGeneratorService, PasswordGeneratorService>();
        services.AddScoped<IKeyDerivationService, KeyDerivationService>();

        AddAllOfType<IPasswordGeneratorTokenProcessor>(services);

        return services;
    }

    private static void AddAllOfType<T>(IServiceCollection services)
    {
        var assembly = typeof(T).Assembly;
        var allTypes = assembly.GetTypes().Where(x => typeof(T).IsAssignableFrom(x) && !x.IsInterface).ToList();
        foreach (var curType in allTypes)
        {
            services.AddScoped(typeof(T), curType);
        }
    }

}
