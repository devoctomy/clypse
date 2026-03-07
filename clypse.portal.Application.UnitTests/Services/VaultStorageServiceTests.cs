using System.Text.Json;
using clypse.portal.Application.Services;
using clypse.portal.Models.Vault;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using Moq;

namespace clypse.portal.Application.UnitTests.Services;

public class VaultStorageServiceTests
{
    private const string VaultsKey = "clypse_vaults";

    private readonly Mock<IJSRuntime> mockJsRuntime;

    public VaultStorageServiceTests()
    {
        this.mockJsRuntime = new Mock<IJSRuntime>();
    }

    private VaultStorageService CreateSut()
    {
        return new VaultStorageService(this.mockJsRuntime.Object);
    }

    private void SetupGetVaults(IEnumerable<VaultMetadata>? vaults)
    {
        string json;
        if (vaults == null)
        {
            json = string.Empty;
        }
        else
        {
            var storage = new VaultStorage { Vaults = vaults.ToList() };
            json = JsonSerializer.Serialize(storage, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            });
        }

        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<string>("localStorage.getItem", It.Is<object?[]>(args => (string?)args[0] == VaultsKey)))
            .ReturnsAsync(json);
    }

    private void SetupSetVaults()
    {
        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<IJSVoidResult>("localStorage.setItem", It.IsAny<object?[]>()))
            .ReturnsAsync(Mock.Of<IJSVoidResult>());
    }

    [Fact]
    public void GivenNullJSRuntime_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new VaultStorageService(null!));
        Assert.Equal("jsRuntime", exception.ParamName);
    }

    [Fact]
    public void GivenValidJSRuntime_WhenConstructing_ThenCreatesInstance()
    {
        // Act
        var sut = this.CreateSut();

        // Assert
        Assert.NotNull(sut);
    }

    [Fact]
    public async Task GivenStoredVaults_WhenGetVaultsAsync_ThenReturnsVaultList()
    {
        // Arrange
        var expected = new List<VaultMetadata>
        {
            new () { Id = "vault-1", Name = "Vault One" },
            new () { Id = "vault-2", Name = "Vault Two" },
        };
        this.SetupGetVaults(expected);

        var sut = this.CreateSut();

        // Act
        var result = await sut.GetVaultsAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, v => v.Id == "vault-1");
        Assert.Contains(result, v => v.Id == "vault-2");
    }

    [Fact]
    public async Task GivenNoStoredVaults_WhenGetVaultsAsync_ThenReturnsEmptyList()
    {
        // Arrange
        this.SetupGetVaults(null);

        var sut = this.CreateSut();

        // Act
        var result = await sut.GetVaultsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GivenInvalidJson_WhenGetVaultsAsync_ThenReturnsEmptyList()
    {
        // Arrange
        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<string>("localStorage.getItem", It.IsAny<object?[]>()))
            .ReturnsAsync("not valid json {{");

        var sut = this.CreateSut();

        // Act
        var result = await sut.GetVaultsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GivenJsException_WhenGetVaultsAsync_ThenReturnsEmptyList()
    {
        // Arrange
        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<string>("localStorage.getItem", It.IsAny<object?[]>()))
            .ThrowsAsync(new JSException("Storage error"));

        var sut = this.CreateSut();

        // Act
        var result = await sut.GetVaultsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GivenVaultList_WhenSaveVaultsAsync_ThenSerializesAndStores()
    {
        // Arrange
        string? capturedJson = null;
        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<IJSVoidResult>("localStorage.setItem", It.IsAny<object?[]>()))
            .Callback<string, object?[]>((_, args) => capturedJson = args[1] as string)
            .ReturnsAsync(Mock.Of<IJSVoidResult>());

        var sut = this.CreateSut();
        var vaults = new List<VaultMetadata>
        {
            new () { Id = "vault-1", Name = "Vault One" },
        };

        // Act
        await sut.SaveVaultsAsync(vaults);

        // Assert
        Assert.NotNull(capturedJson);
        Assert.Contains("vault-1", capturedJson);
        this.mockJsRuntime.Verify(
            x => x.InvokeAsync<IJSVoidResult>("localStorage.setItem", It.Is<object?[]>(args => (string?)args[0] == VaultsKey)),
            Times.Once);
    }

    [Fact]
    public async Task GivenJsException_WhenSaveVaultsAsync_ThenHandlesSilently()
    {
        // Arrange
        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<IJSVoidResult>("localStorage.setItem", It.IsAny<object?[]>()))
            .ThrowsAsync(new JSException("Storage full"));

        var sut = this.CreateSut();

        // Act & Assert
        var exception = await Record.ExceptionAsync(() =>
            sut.SaveVaultsAsync([new VaultMetadata { Id = "v1" }]));

        Assert.Null(exception);
    }

    [Fact]
    public async Task GivenExistingVault_WhenUpdateVaultAsync_ThenUpdatesVaultInPlace()
    {
        // Arrange
        var existing = new List<VaultMetadata>
        {
            new () { Id = "vault-1", Name = "Old Name", Description = "Old Desc" },
        };
        this.SetupGetVaults(existing);
        this.SetupSetVaults();

        string? savedJson = null;
        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<IJSVoidResult>("localStorage.setItem", It.IsAny<object?[]>()))
            .Callback<string, object?[]>((_, args) => savedJson = args[1] as string)
            .ReturnsAsync(Mock.Of<IJSVoidResult>());

        var sut = this.CreateSut();
        var updated = new VaultMetadata { Id = "vault-1", Name = "New Name", Description = "New Desc" };

        // Act
        await sut.UpdateVaultAsync(updated);

        // Assert
        Assert.NotNull(savedJson);
        Assert.Contains("New Name", savedJson);
    }

    [Fact]
    public async Task GivenNewVault_WhenUpdateVaultAsync_ThenAddsVaultToList()
    {
        // Arrange
        var existing = new List<VaultMetadata>
        {
            new () { Id = "vault-1", Name = "Vault One" },
        };
        this.SetupGetVaults(existing);

        string? savedJson = null;
        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<IJSVoidResult>("localStorage.setItem", It.IsAny<object?[]>()))
            .Callback<string, object?[]>((_, args) => savedJson = args[1] as string)
            .ReturnsAsync(Mock.Of<IJSVoidResult>());

        var sut = this.CreateSut();
        var newVault = new VaultMetadata { Id = "vault-2", Name = "Vault Two" };

        // Act
        await sut.UpdateVaultAsync(newVault);

        // Assert
        Assert.NotNull(savedJson);
        Assert.Contains("vault-1", savedJson);
        Assert.Contains("vault-2", savedJson);
    }

    [Fact]
    public async Task GivenExistingVault_WhenRemoveVaultAsync_ThenRemovesFromList()
    {
        // Arrange
        var existing = new List<VaultMetadata>
        {
            new () { Id = "vault-1", Name = "Vault One" },
            new () { Id = "vault-2", Name = "Vault Two" },
        };
        this.SetupGetVaults(existing);

        string? savedJson = null;
        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<IJSVoidResult>("localStorage.setItem", It.IsAny<object?[]>()))
            .Callback<string, object?[]>((_, args) => savedJson = args[1] as string)
            .ReturnsAsync(Mock.Of<IJSVoidResult>());

        var sut = this.CreateSut();

        // Act
        await sut.RemoveVaultAsync("vault-1");

        // Assert
        Assert.NotNull(savedJson);
        Assert.DoesNotContain("vault-1", savedJson);
        Assert.Contains("vault-2", savedJson);
    }

    [Fact]
    public async Task GivenNonExistentVaultId_WhenRemoveVaultAsync_ThenDoesNothing()
    {
        // Arrange
        var existing = new List<VaultMetadata>
        {
            new () { Id = "vault-1", Name = "Vault One" },
        };
        this.SetupGetVaults(existing);

        var sut = this.CreateSut();

        // Act & Assert
        var exception = await Record.ExceptionAsync(() => sut.RemoveVaultAsync("non-existent-id"));
        Assert.Null(exception);

        // SaveVaults should NOT have been called since the vault wasn't found
        this.mockJsRuntime.Verify(
            x => x.InvokeAsync<IJSVoidResult>("localStorage.setItem", It.IsAny<object?[]>()),
            Times.Never);
    }

    [Fact]
    public async Task GivenVaults_WhenClearVaultsAsync_ThenRemovesKey()
    {
        // Arrange
        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<IJSVoidResult>("localStorage.removeItem", It.IsAny<object?[]>()))
            .ReturnsAsync(Mock.Of<IJSVoidResult>());

        var sut = this.CreateSut();

        // Act
        await sut.ClearVaultsAsync();

        // Assert
        this.mockJsRuntime.Verify(
            x => x.InvokeAsync<IJSVoidResult>("localStorage.removeItem", It.Is<object?[]>(args => (string?)args[0] == VaultsKey)),
            Times.Once);
    }

    [Fact]
    public async Task GivenJsException_WhenClearVaultsAsync_ThenHandlesSilently()
    {
        // Arrange
        this.mockJsRuntime
            .Setup(x => x.InvokeAsync<IJSVoidResult>("localStorage.removeItem", It.IsAny<object?[]>()))
            .ThrowsAsync(new JSException("Error"));

        var sut = this.CreateSut();

        // Act & Assert
        var exception = await Record.ExceptionAsync(() => sut.ClearVaultsAsync());
        Assert.Null(exception);
    }
}
