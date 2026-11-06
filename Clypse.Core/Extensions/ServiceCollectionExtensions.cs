using Clypse.Core.Configuration;
using Clypse.Core.Cryptography;
using Clypse.Core.Data;
using Clypse.Core.Password;
using Clypse.Core.Secrets.Import;
using Microsoft.Extensions.DependencyInjection;

namespace Clypse.Core.Extensions;

/// <summary>
/// Extension methods for IServiceCollection to add Clypse core services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Clypse core services: password generation and complexity estimation, the embedded text lists, and
    /// secret import.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configureTextLists">Optionally sets which text lists are loaded in full (see
    /// <see cref="TextListOptions"/>).</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddClypseCore(
        this IServiceCollection services,
        Action<TextListOptions>? configureTextLists = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var textLists = services.AddOptions<TextListOptions>();
        if (configureTextLists != null)
        {
            textLists.Configure(configureTextLists);
        }

        // Keeps each list once loaded, for the lifetime of the app.
        services.AddSingleton<IEmbeddedResourceLoaderService, EmbeddedResourceLoaderService>();
        services.AddScoped<IRandomGeneratorService, RandomGeneratorService>();
        services.AddScoped<IPasswordGeneratorService, StandardWesternPasswordGeneratorService>();
        services.AddScoped<IWeakKnownPasswordsService, WeakKnownPasswordsService>();
        services.AddScoped<IPasswordComplexityEstimatorService, StandardWesternPasswordComplexityEstimatorService>();
        services.AddScoped<ISecretsImporterService, CsvSecretsImporterService>();
        services.AddScoped<ISsoProvidersService, SsoProvidersService>();

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
