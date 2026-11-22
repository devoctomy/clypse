using Clypse.Portal.Application.Services;
using Clypse.Portal.Application.Text;
using Devoctomy.Blazwiz.Testing;
using Devoctomy.Blazwiz.UI.Models;
using Devoctomy.Blazwiz.UI.Services.Interfaces;
using Moq;

namespace Clypse.Portal.Application.UnitTests.Services;

public class VersionFooterServiceTests
{
    private readonly FrameworkText text = new(typeof(ClypseText));
    private readonly Mock<IAppBuildInfo> mockBuildInfo = new();
    private readonly AppSettings appSettings = new() { CopyrightMessage = "© 2026 Clypse" };

    public VersionFooterServiceTests()
    {
        mockBuildInfo.SetupGet(x => x.Version).Returns("0.9.2");
    }

    [Fact]
    public void GivenNullDependencies_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Equal("appSettings", Assert.Throws<ArgumentNullException>(() => new VersionFooterService(null!, mockBuildInfo.Object, text.For<ClypseText>())).ParamName);
        Assert.Equal("buildInfo", Assert.Throws<ArgumentNullException>(() => new VersionFooterService(appSettings, null!, text.For<ClypseText>())).ParamName);
        Assert.Equal("text", Assert.Throws<ArgumentNullException>(() => new VersionFooterService(appSettings, mockBuildInfo.Object, null!)).ParamName);
    }

    [Fact]
    public void GivenACopyrightMessage_WhenApplied_ThenTheVersionComesBeforeIt()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.Apply();

        // Assert
        Assert.Equal("Version 0.9.2 - © 2026 Clypse", appSettings.CopyrightMessage);
    }

    [Theory]
    [InlineData("0.9.2.0", "0.9.2")]
    [InlineData("0.9.2.7", "0.9.2.7")]
    [InlineData("0.9.2-local.1", "0.9.2-local.1")]
    public void GivenAVersion_WhenApplied_ThenAnUnusedFourthPartIsLeftOff(string version, string expected)
    {
        // Arrange
        mockBuildInfo.SetupGet(x => x.Version).Returns(version);
        var sut = CreateSut();

        // Act
        sut.Apply();

        // Assert
        Assert.Equal($"Version {expected} - © 2026 Clypse", appSettings.CopyrightMessage);
    }

    [Fact]
    public void GivenNoCopyrightMessage_WhenApplied_ThenTheVersionIsShownAlone()
    {
        // Arrange
        appSettings.CopyrightMessage = string.Empty;
        var sut = CreateSut();

        // Act
        sut.Apply();

        // Assert
        Assert.Equal("Version 0.9.2", appSettings.CopyrightMessage);
    }

    [Fact]
    public void GivenItWasApplied_WhenAppliedAgain_ThenTheVersionIsNotAddedTwice()
    {
        // Arrange
        var sut = CreateSut();
        sut.Apply();

        // Act
        sut.Apply();

        // Assert
        Assert.Equal("Version 0.9.2 - © 2026 Clypse", appSettings.CopyrightMessage);
    }

    [Fact]
    public void GivenNoVersion_WhenApplied_ThenTheCopyrightMessageIsLeftAsItIs()
    {
        // Arrange
        mockBuildInfo.SetupGet(x => x.Version).Returns(string.Empty);
        var sut = CreateSut();

        // Act
        sut.Apply();

        // Assert
        Assert.Equal("© 2026 Clypse", appSettings.CopyrightMessage);
    }

    private VersionFooterService CreateSut() => new(appSettings, mockBuildInfo.Object, text.For<ClypseText>());
}
