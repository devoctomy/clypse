using Clypse.Portal.Application.Extensions;
using Clypse.Portal.Application.Services;
using Clypse.Portal.Application.Services.Interfaces;
using Clypse.Portal.Application.Text;
using Devoctomy.Blazwiz.Auth.Sessions.Interfaces;
using Devoctomy.Blazwiz.UI.Text;
using Microsoft.Extensions.DependencyInjection;

namespace Clypse.Portal.Application.UnitTests.Extensions;

public class ClypseServiceCollectionExtensionsTests
{
    [Fact]
    public void GivenServices_WhenAddingClypse_ThenVaultsSecretsAndTheirErrorTextAreRegistered()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddClypse();

        // Assert
        AssertScoped<IVaultService, VaultService>(services);
        AssertScoped<ISecretServiceFactory, SecretServiceFactory>(services);
        AssertScoped<IVaultSession, VaultSession>(services);
        Assert.Contains(services, d => d.ServiceType == typeof(ISessionLifecycleHandler) && d.ImplementationType == typeof(VaultLabelHandler));
        Assert.Contains(services, d => d.ImplementationInstance is ErrorTextSource source && source.ResourceType == typeof(ClypseText));
        Assert.Contains(services, d => d.ServiceType == typeof(TimeProvider));
    }

    [Fact]
    public void GivenATimeProviderAlready_WhenAddingClypse_ThenItIsKept()
    {
        // Arrange
        var services = new ServiceCollection();
        var clock = new Microsoft.Extensions.Time.Testing.FakeTimeProvider();
        services.AddSingleton<TimeProvider>(clock);

        // Act
        services.AddClypse();

        // Assert
        Assert.Same(clock, Assert.Single(services, d => d.ServiceType == typeof(TimeProvider)).ImplementationInstance);
    }

    [Fact]
    public void GivenNoServices_WhenAddingClypse_ThenThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ClypseServiceCollectionExtensions.AddClypse(null!));
    }

    private static void AssertScoped<TService, TImplementation>(IServiceCollection services)
    {
        var descriptor = Assert.Single(services, d => d.ServiceType == typeof(TService));
        Assert.Equal(typeof(TImplementation), descriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }
}
