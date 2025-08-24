using clypse.core.Cloud.Interfaces;
using clypse.core.Compression.Interfaces;
using clypse.core.Secrets;
using clypse.core.Vault;
using Moq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace clypse.core.UnitTests.Vault;

public class VaultManagerTests
{
    private readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };

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
    public async Task GivenVault_AndAddSecret_WhenSaveAsync_ThenInfoSaved_AndSecretSaved_AndIndexSaved_AndIndexUpdated()
    {
        // Arrange
        var name = "Foobar";
        var description = "Description of vault";
        var base64Key = "super secret base64 encoded encryption key";
        var vault = _sut.Create(name, description);
        var cancellationTokenSource = new CancellationTokenSource();

        var secret1 = new core.Secrets.Secret
        {
            Name = "Secret1",
            Description = description,
        };
        vault.AddSecret(secret1);

        // Act
        var results = await _sut.SaveAsync(
            vault,
            base64Key,
            cancellationTokenSource.Token);

        // Assert
        Assert.True(results.Success);
        Assert.Equal(1, results.SecretsCreated);
        Assert.Equal(0, results.SecretsUpdated);
        Assert.Equal(0, results.SecretsDeleted);
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

    [Fact]
    public async Task GivenVault_AndAddSecret_AndVaultSaved_WhenUpdateSecretAsync_AndVaultSaved_ThenInfoSaved_AndSecretSaved_AndIndexSaved_AndIndexUpdated()
    {
        // Arrange
        var name = "Foobar";
        var description = "Description of vault";
        var updatedDescription = "Hello World!";
        var base64Key = "super secret base64 encoded encryption key";
        var vault = _sut.Create(name, description);
        var cancellationTokenSource = new CancellationTokenSource();

        var secret1 = new core.Secrets.Secret
        {
            Name = "Secret1",
            Description = description
        };
        vault.AddSecret(secret1);
        await _sut.SaveAsync(
            vault,
            base64Key,
            cancellationTokenSource.Token);

        _mockEncryptedCloudStorageProvider.Reset();

        // Act
        secret1.Description = updatedDescription;
        vault.UpdateSecret(secret1);
        var results = await _sut.SaveAsync(
            vault,
            base64Key,
            cancellationTokenSource.Token);

        // Assert
        Assert.True(results.Success);
        Assert.Equal(0, results.SecretsCreated);
        Assert.Equal(1, results.SecretsUpdated);
        Assert.Equal(0, results.SecretsDeleted);
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

    [Fact]
    public async Task GivenVault_WhenVerifyAsync_ThenVaultVerified_AndResultsReturned()
    {
        // Arrange
        var vault = new Mock<IVault>();
        var name = "Foobar";
        var description = "Description of vault";
        var base64Key = "super secret base64 encoded encryption key";
        var entries = new List<VaultIndexEntry>()
        {
            new("1",
                "Secret1",
                "Description of Secret1.",
                "apple,orange,pear"),
            new("2",
                "Secret2",
                "Description of Secret2.",
                "red,green,blue")
        };
        var info = new VaultInfo(
            name,
            description);
        var index = new VaultIndex
        {
            Entries = entries
        };

        vault.SetupGet(x => x.Info)
            .Returns(info);

        vault.SetupGet(x => x.Index)
            .Returns(index);

        _mockEncryptedCloudStorageProvider.Setup(x => x.GetEncryptedObjectAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((string key, string base64EncryptionKey, CancellationToken ct) =>
            {
                var id = key.Split('/').Last();
                var indexEntry = entries.SingleOrDefault(y => y.Id == id);
                var secret = new Secret
                {
                    Id = id,
                    Name = indexEntry!.Name,
                    Description = indexEntry!.Description,
                };
                var stream = new MemoryStream();
                JsonSerializer.Serialize(stream, secret, JsonSerializerOptions);
                return stream;
            });

        _mockCompressionService.Setup(x => x.DecompressAsync(
            It.IsAny<Stream>(),
            It.IsAny<Stream>(),
            It.IsAny<CancellationToken>()))
            .Callback(async (Stream input, Stream output, CancellationToken ct) =>
            {
                await input.CopyToAsync(output);
                await output.FlushAsync();
            });

        _mockEncryptedCloudStorageProvider.Setup(x => x.ListObjectsAsync(
            $"{info.Id}/secrets/",
            CancellationToken.None))
            .ReturnsAsync(entries.Select(x => $"{info.Id}/secrets/x.Id").ToList());

        // Act
        var results = await _sut.VerifyAsync(
            vault.Object,
            base64Key,
            CancellationToken.None);

        // Assert
        Assert.True(results.Success);
    }
}
