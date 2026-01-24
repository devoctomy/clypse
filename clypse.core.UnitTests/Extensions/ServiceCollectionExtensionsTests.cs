using clypse.core.Cryptography;
using clypse.core.Cryptography.Interfaces;
using clypse.core.Data;
using clypse.core.Extensions;
using clypse.core.Password;
using clypse.core.Secrets.Import;
using Microsoft.Extensions.DependencyInjection;

namespace clypse.core.UnitTests.Extensions;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void GivenServiceCollection_WhenAddClypseCoreServices_ThenExpectedServicesAreRegistered()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(KeyDerivationServiceDefaultOptions.Blazor_Argon2id_Test());

        // Act
        services.AddClypseCoreServices();
        var provider = services.BuildServiceProvider();

        // Assert
        Assert.IsType<RandomGeneratorService>(provider.GetRequiredService<IRandomGeneratorService>());
        Assert.IsType<StandardWesternPasswordGeneratorService>(provider.GetRequiredService<IPasswordGeneratorService>());
        Assert.IsType<KeyDerivationService>(provider.GetRequiredService<IKeyDerivationService>());
        Assert.IsType<StandardWesternPasswordComplexityEstimatorService>(provider.GetRequiredService<IPasswordComplexityEstimatorService>());
        Assert.IsType<CsvSecretsImporterService>(provider.GetRequiredService<ISecretsImporterService>());
        Assert.IsType<EmbeddedResorceLoaderService>(provider.GetRequiredService<IEmbeddedResorceLoaderService>());
    }

    [Fact]
    public void GivenServiceCollection_WhenAddClypseCoreServices_ThenTokenProcessorsRegisteredFromAssembly()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddClypseCoreServices();
        var provider = services.BuildServiceProvider();
        var tokenProcessors = provider.GetServices<IPasswordGeneratorTokenProcessor>().ToList();

        // Assert
        Assert.Equal(2, tokenProcessors.Count);
        Assert.Contains(tokenProcessors, x => x is DictionaryTokenProcessor);
        Assert.Contains(tokenProcessors, x => x is RandomStringTokenProcessor);
    }
}
