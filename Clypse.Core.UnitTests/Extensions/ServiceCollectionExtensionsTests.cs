using Clypse.Core.Cryptography;
using Clypse.Core.Configuration;
using Clypse.Core.Data;
using Clypse.Core.Extensions;
using Clypse.Core.Password;
using Clypse.Core.Secrets.Import;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Clypse.Core.UnitTests.Extensions;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void GivenServiceCollection_WhenAddClypseCore_ThenExpectedServicesAreRegistered()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddClypseCore();
        var provider = services.BuildServiceProvider();

        // Assert
        Assert.IsType<RandomGeneratorService>(provider.GetRequiredService<IRandomGeneratorService>());
        Assert.IsType<StandardWesternPasswordGeneratorService>(provider.GetRequiredService<IPasswordGeneratorService>());
        Assert.IsType<WeakKnownPasswordsService>(provider.GetRequiredService<IWeakKnownPasswordsService>());
        Assert.IsType<SsoProvidersService>(provider.GetRequiredService<ISsoProvidersService>());
        Assert.IsType<StandardWesternPasswordComplexityEstimatorService>(provider.GetRequiredService<IPasswordComplexityEstimatorService>());
        Assert.IsType<CsvSecretsImporterService>(provider.GetRequiredService<ISecretsImporterService>());
        Assert.IsType<EmbeddedResourceLoaderService>(provider.GetRequiredService<IEmbeddedResourceLoaderService>());
        Assert.True(provider.GetRequiredService<IOptions<TextListOptions>>().Value.UseFullWeakKnownPasswordsList);
    }

    [Fact]
    public void GivenServiceCollection_WhenAddClypseCoreWithTextListsConfigured_ThenOptionsApplied()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddClypseCore(textLists => textLists.UseFullWeakKnownPasswordsList = false);
        var provider = services.BuildServiceProvider();

        // Assert
        Assert.False(provider.GetRequiredService<IOptions<TextListOptions>>().Value.UseFullWeakKnownPasswordsList);
    }

    [Fact]
    public void GivenServiceCollection_WhenAddClypseCore_ThenResourceLoaderIsShared()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddClypseCore();
        var provider = services.BuildServiceProvider();

        // Act
        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();
        var first = firstScope.ServiceProvider.GetRequiredService<IEmbeddedResourceLoaderService>();
        var second = secondScope.ServiceProvider.GetRequiredService<IEmbeddedResourceLoaderService>();

        // Assert
        Assert.Same(first, second);
    }

    [Fact]
    public void GivenServiceCollection_WhenAddClypseCore_ThenTokenProcessorsRegisteredFromAssembly()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddClypseCore();
        var provider = services.BuildServiceProvider();
        var tokenProcessors = provider.GetServices<IPasswordGeneratorTokenProcessor>().ToList();

        // Assert
        Assert.Equal(2, tokenProcessors.Count);
        Assert.Contains(tokenProcessors, x => x is DictionaryTokenProcessor);
        Assert.Contains(tokenProcessors, x => x is RandomStringTokenProcessor);
    }
}
