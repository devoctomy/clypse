using Clypse.Portal.Application.Extensions;
using Clypse.Portal.Application.Services;
using Clypse.Portal.Application.Services.Interfaces;
using Clypse.Portal.Application.Text;
using Clypse.Portal.Models.Settings;
using Devoctomy.Blazwiz.Auth.Sessions.Interfaces;
using Devoctomy.Blazwiz.UI.Models;
using Devoctomy.Blazwiz.UI.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace Clypse.Portal.Application.UnitTests.Extensions;

public class ClypseServiceCollectionExtensionsTests
{
    private readonly IConfiguration emptyConfiguration = new ConfigurationBuilder().Build();

    [Fact]
    public void GivenServices_WhenAddingClypse_ThenVaultsSecretsAndTheirErrorTextAreRegistered()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddClypse(emptyConfiguration);

        // Assert
        AssertScoped<IVaultService, VaultService>(services);
        AssertScoped<ISecretServiceFactory, SecretServiceFactory>(services);
        AssertScoped<IVaultSession, VaultSession>(services);
        AssertScoped<IPasswordStrengthService, PasswordStrengthService>(services);
        Assert.Contains(services, d => d.ServiceType == typeof(ISessionLifecycleHandler) && d.ImplementationType == typeof(VaultLabelHandler));
        Assert.Contains(services, d => d.ImplementationInstance is ErrorTextSource source && source.ResourceType == typeof(ClypseText));
        Assert.Contains(services, d => d.ServiceType == typeof(TimeProvider));
    }

    [Fact]
    public void GivenServices_WhenAddingClypse_ThenTheVaultsPageIsInTheSidebar()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddClypse(emptyConfiguration);

        // Assert
        var item = Assert.Single(services.Select(d => d.ImplementationInstance).OfType<NavigationItem>());
        Assert.Equal("vaults", item.Href);
        Assert.Equal("Nav.Vaults", item.Text);
        Assert.Equal(typeof(ClypseText), item.TextResource);
    }

    [Fact]
    public void GivenAClypseSection_WhenAddingClypse_ThenItsSettingsAreRegistered()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Clypse:MemorablePasswordTemplates:0:Name"] = "Words",
                ["Clypse:MemorablePasswordTemplates:0:Template"] = "{dict(noun):random}",
            })
            .Build();
        var services = new ServiceCollection();

        // Act
        services.AddClypse(configuration);

        // Assert
        var settings = Assert.IsType<ClypseSettings>(Assert.Single(services, d => d.ServiceType == typeof(ClypseSettings)).ImplementationInstance);
        var template = Assert.Single(settings.MemorablePasswordTemplates);
        Assert.Equal("Words", template.Name);
        Assert.Equal("{dict(noun):random}", template.Template);
    }

    [Fact]
    public void GivenNoClypseSection_WhenAddingClypse_ThenDefaultSettingsAreRegistered()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddClypse(emptyConfiguration);

        // Assert
        var settings = Assert.IsType<ClypseSettings>(Assert.Single(services, d => d.ServiceType == typeof(ClypseSettings)).ImplementationInstance);
        Assert.Empty(settings.MemorablePasswordTemplates);
    }

    [Fact]
    public void GivenATimeProviderAlready_WhenAddingClypse_ThenItIsKept()
    {
        // Arrange
        var services = new ServiceCollection();
        var clock = new FakeTimeProvider();
        services.AddSingleton<TimeProvider>(clock);

        // Act
        services.AddClypse(emptyConfiguration);

        // Assert
        Assert.Same(clock, Assert.Single(services, d => d.ServiceType == typeof(TimeProvider)).ImplementationInstance);
    }

    [Fact]
    public void GivenNullArguments_WhenAddingClypse_ThenThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Equal("services", Assert.Throws<ArgumentNullException>(() => ClypseServiceCollectionExtensions.AddClypse(null!, emptyConfiguration)).ParamName);
        Assert.Equal("configuration", Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddClypse(null!)).ParamName);
    }

    private static void AssertScoped<TService, TImplementation>(IServiceCollection services)
    {
        var descriptor = Assert.Single(services, d => d.ServiceType == typeof(TService));
        Assert.Equal(typeof(TImplementation), descriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }
}
