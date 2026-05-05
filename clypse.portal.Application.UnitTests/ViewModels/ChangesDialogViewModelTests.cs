using clypse.portal.Application.ViewModels;
using clypse.portal.Models.Settings;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using Moq.Protected;

namespace clypse.portal.Application.UnitTests.ViewModels;

public class ChangesDialogViewModelTests
{
    private readonly Mock<ILogger<ChangesDialogViewModel>> mockLogger;
    private readonly AppSettings defaultAppSettings;

    public ChangesDialogViewModelTests()
    {
        this.mockLogger = new Mock<ILogger<ChangesDialogViewModel>>();
        this.defaultAppSettings = new AppSettings();
    }

    private static HttpClient CreateHttpClientWithResponse(
        string content,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content)
            });
        return new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost/") };
    }

    private ChangesDialogViewModel CreateSut(HttpClient? httpClient = null, AppSettings? appSettings = null)
    {
        httpClient ??= CreateHttpClientWithResponse("{}");
        appSettings ??= this.defaultAppSettings;
        return new ChangesDialogViewModel(httpClient, this.mockLogger.Object, appSettings);
    }

    [Fact]
    public async Task GivenChangesJson_AndSingleVersion_EnsureChangelogLoadedAsync_ThenSingleVersionReturned()
    {
        // Arrange
        var json = """{"versions":[{"version":"1.0","changes":[{"type":"feature","description":"First release"}]}]}""";
        var httpClient = CreateHttpClientWithResponse(json);
        var sut = this.CreateSut(httpClient);

        // Act
        await sut.EnsureChangelogLoadedAsync();

        // Assert
        Assert.NotNull(sut.ChangeLog);
        Assert.Single(sut.ChangeLog.Versions);
    }

    [Fact]
    public async Task GivenChangelogAlreadyLoaded_WhenEnsureChangelogLoadedAsync_ThenDoesNotReload()
    {
        // Arrange
        var callCount = 0;
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("""{"versions":[]}""")
                };
            });
        var httpClient = new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost/") };
        var sut = this.CreateSut(httpClient);
        await sut.EnsureChangelogLoadedAsync();

        // Act
        await sut.EnsureChangelogLoadedAsync();

        // Assert
        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task GivenChangeLogUnavailable_WhenEnsureChangelogLoadedAsync_ThenSetsErrorMessage()
    {
        // Arrange
        var httpClient = CreateHttpClientWithResponse(string.Empty, HttpStatusCode.InternalServerError);
        var sut = this.CreateSut(httpClient);

        // Act
        await sut.EnsureChangelogLoadedAsync();

        // Assert
        Assert.NotNull(sut.ErrorMessage);
        Assert.Null(sut.ChangeLog);
    }

    [Fact]
    public async Task GivenInstance_WhenHandleCloseCommand_ThenCallbackInvoked()
    {
        // Arrange
        var sut = this.CreateSut();
        var called = false;
        sut.OnCloseCallback = () => { called = true; return Task.CompletedTask; };

        // Act
        await sut.HandleCloseCommand.ExecuteAsync(null);

        // Assert
        Assert.True(called);
    }

    [Fact]
    public async Task GivenInstance_WhenHandleUpdateCommand_ThenCallbackInvoked()
    {
        // Arrange
        var sut = this.CreateSut();
        var called = false;
        sut.OnUpdateCallback = () => { called = true; return Task.CompletedTask; };

        // Act
        await sut.HandleUpdateCommand.ExecuteAsync(null);

        // Assert
        Assert.True(called);
    }

    [Fact]
    public void GivenAppSettingsWithDeployment_WhenConstructed_ThenAppSettingsExposed()
    {
        // Arrange
        var appSettings = new AppSettings
        {
            Deployment = new DeploymentSettings
            {
                DeployedBy = "testuser",
                DeployedAt = "2026-05-05T07:00:00Z",
                DeploymentActionUrl = "https://github.com/test/repo/actions/runs/123"
            }
        };
        var sut = this.CreateSut(appSettings: appSettings);

        // Assert
        Assert.NotNull(sut.AppSettings);
        Assert.NotNull(sut.AppSettings.Deployment);
        Assert.Equal("testuser", sut.AppSettings.Deployment.DeployedBy);
        Assert.Equal("2026-05-05T07:00:00Z", sut.AppSettings.Deployment.DeployedAt);
        Assert.Equal("https://github.com/test/repo/actions/runs/123", sut.AppSettings.Deployment.DeploymentActionUrl);
        Assert.True(sut.AppSettings.Deployment.HasAnyData);
    }

    [Fact]
    public void GivenAppSettingsWithoutDeployment_WhenConstructed_ThenDeploymentIsNull()
    {
        // Arrange
        var appSettings = new AppSettings();
        var sut = this.CreateSut(appSettings: appSettings);

        // Assert
        Assert.NotNull(sut.AppSettings);
        Assert.Null(sut.AppSettings.Deployment);
    }

    [Fact]
    public void GivenDeploymentSettingsWithNoData_WhenHasAnyDataChecked_ThenReturnsFalse()
    {
        // Arrange
        var deployment = new DeploymentSettings();

        // Assert
        Assert.False(deployment.HasAnyData);
    }

    [Fact]
    public void GivenDeploymentSettingsWithDeployedByOnly_WhenHasAnyDataChecked_ThenReturnsTrue()
    {
        // Arrange
        var deployment = new DeploymentSettings { DeployedBy = "testuser" };

        // Assert
        Assert.True(deployment.HasAnyData);
    }
}
