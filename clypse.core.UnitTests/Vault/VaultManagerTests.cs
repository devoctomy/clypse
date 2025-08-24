using clypse.core.Cloud.Interfaces;
using clypse.core.Compression.Interfaces;
using clypse.core.Vault;
using Moq;

namespace clypse.core.UnitTests.Vault;

public class VaultManagerTests
{
    private readonly Mock<ICompressionService> _mockCompressionService;
    private readonly Mock<IEncryptedCloudStorageProvider> _mockEncryptedCloudStorageProvider;
    private readonly IVaultManager _sut;

    public VaultManagerTests()
    {
        _mockCompressionService = new Mock<ICompressionService>();
        _mockEncryptedCloudStorageProvider = new Mock<IEncryptedCloudStorageProvider>();
        _sut = new VaultManager(
            _mockCompressionService.Object,
            _mockEncryptedCloudStorageProvider.Object);
    }

    [Fact]
    public void GivenName_AndDescription_WhenCreate_ThenVaultCreated()
    {
        // Arrange
        var name = "Foobar";
        var description = "Description of vault";

        // Act
        var vault = _sut.Create(name, description);

        // Assert
        Assert.Equal(name, vault.Info.Name);
        Assert.Equal(description, vault.Info.Description);
    }

    [Fact]
    public async Task GivenVault_AndSaveAsync_ThenInfoSaved_AndIndexSaved()
    {
        // Arrange
        var name = "Foobar";
        var description = "Description of vault";
        var base64Key = "super secret base64 encoded encryption key";
        var vault = _sut.Create(name, description);
        var cancellationTokenSource = new CancellationTokenSource();

        // Act
        await _sut.SaveAsync(
            vault,
            base64Key,
            cancellationTokenSource.Token);

        // Assert
        _mockEncryptedCloudStorageProvider.Verify(x => x.PutEncryptedObjectAsync(
            It.Is<string>(y => y == $"{vault.Info.Id}/info.json"),
            It.IsAny<Stream>(),
            It.Is<string>(y => y == base64Key),
            It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)), Times.Once);
        _mockEncryptedCloudStorageProvider.Verify(x => x.PutEncryptedObjectAsync(
            It.Is<string>(y => y == $"{vault.Info.Id}/index.json"),
            It.IsAny<Stream>(),
            It.Is<string>(y => y == base64Key),
            It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)), Times.Once);
    }

    [Fact]
    public async Task GivenVault_AndAddSecret_AndSaveAsync_ThenInfoSaved_AndSecretSaved_AndIndexSaved_AndIndexUpdated()
    {
        // Arrange
        var name = "Foobar";
        var description = "Description of vault";
        var base64Key = "super secret base64 encoded encryption key";
        var vault = _sut.Create(name, description);
        var cancellationTokenSource = new CancellationTokenSource();

        var secret1 = new core.Secrets.Secret
        {
            Name = "Secret1"
        };
        vault.AddSecret(secret1);

        // Act
        await _sut.SaveAsync(
            vault,
            base64Key,
            cancellationTokenSource.Token);

        // Assert
        _mockEncryptedCloudStorageProvider.Verify(x => x.PutEncryptedObjectAsync(
            It.Is<string>(y => y == $"{vault.Info.Id}/info.json"),
            It.IsAny<Stream>(),
            It.Is<string>(y => y == base64Key),
            It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)), Times.Once);
        _mockEncryptedCloudStorageProvider.Verify(x => x.PutEncryptedObjectAsync(
            It.Is<string>(y => y == $"{vault.Info.Id}/secrets/{secret1.Id}"),
            It.IsAny<Stream>(),
            It.Is<string>(y => y == base64Key),
            It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)), Times.Once);
        _mockEncryptedCloudStorageProvider.Verify(x => x.PutEncryptedObjectAsync(
            It.Is<string>(y => y == $"{vault.Info.Id}/index.json"),
            It.IsAny<Stream>(),
            It.Is<string>(y => y == base64Key),
            It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)), Times.Once);
        Assert.Equal(secret1.Id, vault.Index.Entries[0].Id);
        Assert.False(vault.IsDirty);
        Assert.Empty(vault.PendingSecrets);
        Assert.Empty(vault.SecretsToDelete);
    }
}
