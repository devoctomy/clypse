using clypse.core.Cloud.Exceptions;
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
    private readonly VaultManager _sut;

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
    public async Task GivenVault_AndClean_WhenSaveAsync_ThenVaultNotSaved()
    {
        // Arrange
        var base64Key = "super secret base64 encoded encryption key";
        var mockVault = new Mock<IVault>();
        var cancellationTokenSource = new CancellationTokenSource();

        // Act
        var results = await _sut.SaveAsync(
            mockVault.Object,
            base64Key,
            cancellationTokenSource.Token);

        // Assert
        Assert.False(results.Success);
    }

    [Fact]
    public async Task GivenVault_AndDirty_AndSecretsToDelete_WhenSaveAsync_ThenVaultSaved_AndSecretsDeleted()
    {
        // Arrange
        var name = "Foobar";
        var description = "Description of vault";
        var base64Key = "super secret base64 encoded encryption key";
        var mockVault = new Mock<IVault>();
        var info = new VaultInfo(name, description);
        var index = new VaultIndex
        {
            Entries =
                [
                    new VaultIndexEntry(
                        "1",
                        "Foo",
                        "Bar",
                        ""),
                    new VaultIndexEntry(
                        "2",
                        "Foo",
                        "Bar",
                        "")
                ]
        };
        var secretsToDelete = new List<string>(["1", "2", "3"]); // 3 Not indexed
        var cancellationTokenSource = new CancellationTokenSource();

        mockVault.SetupGet(x => x.Info)
            .Returns(info);

        mockVault.SetupGet(x => x.Index)
            .Returns(index);

        mockVault.SetupGet(x => x.IsDirty)
            .Returns(true);

        mockVault.SetupGet(x => x.PendingSecrets)
            .Returns([]);

        mockVault.SetupGet(x => x.SecretsToDelete)
            .Returns(secretsToDelete);

        // Act
        var results = await _sut.SaveAsync(
            mockVault.Object,
            base64Key,
            cancellationTokenSource.Token);

        // Assert
        Assert.True(results.Success);
        Assert.Empty(index.Entries);
        mockVault.Verify(x => x.MakeClean(), Times.Once);
        _mockEncryptedCloudStorageProvider.Verify(x => x.PutEncryptedObjectAsync(
            It.Is<string>(y => y == $"{info.Id}/info.json"),
            It.IsAny<Stream>(),
            It.Is<string>(y => y == base64Key),
            It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)), Times.Once);
        _mockEncryptedCloudStorageProvider.Verify(x => x.DeleteEncryptedObjectAsync(
            It.Is<string>(y => y == $"{info.Id}/secrets/1"),
            It.Is<string>(y => y == base64Key),
            It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)), Times.Once);
        _mockEncryptedCloudStorageProvider.Verify(x => x.DeleteEncryptedObjectAsync(
            It.Is<string>(y => y == $"{info.Id}/secrets/2"),
            It.Is<string>(y => y == base64Key),
            It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)), Times.Once);
        _mockEncryptedCloudStorageProvider.Verify(x => x.PutEncryptedObjectAsync(
            It.Is<string>(y => y == $"{info.Id}/index.json"),
            It.IsAny<Stream>(),
            It.Is<string>(y => y == base64Key),
            It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)), Times.Once);
    }

    [Fact]
    public async Task GivenId_AndBase64Key_WhenLoadAsync_ThenInfoLoaded_AndIndexLoaded_AndVaultReturned()
    {
        // Arrange
        var name = "Foobar";
        var description = "Description of vault";
        var base64Key = "super secret base64 encoded encryption key";
        var info = new VaultInfo(name, description);
        var index = new VaultIndex
        {
            Entries =
                [
                    new VaultIndexEntry(
                        "1",
                        "Foo",
                        "Bar",
                        ""),
                    new VaultIndexEntry(
                        "2",
                        "Foo",
                        "Bar",
                        "")
                ]
        };
        var cancellationTokenSource = new CancellationTokenSource();

        _mockEncryptedCloudStorageProvider.Setup(x => x.GetEncryptedObjectAsync(
            It.Is<string>(y => y == $"{info.Id}/info.json"),
            It.Is<string>(y => y == base64Key),
            It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)))
            .Returns(async (string key, string base64EncryptionKey, CancellationToken ct) =>
            {
                var output = new MemoryStream();
                await JsonSerializer.SerializeAsync(output, info, JsonSerializerOptions, ct);
                return output;
            });

        _mockEncryptedCloudStorageProvider.Setup(x => x.GetEncryptedObjectAsync(
            It.Is<string>(y => y == $"{info.Id}/index.json"),
            It.Is<string>(y => y == base64Key),
            It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)))
            .Returns(async (string key, string base64EncryptionKey, CancellationToken ct) =>
            {
                var output = new MemoryStream();
                await JsonSerializer.SerializeAsync(output, index, JsonSerializerOptions, ct);
                return output;
            });

        _mockCompressionService.Setup(x => x.DecompressAsync(
            It.IsAny<Stream>(),
            It.IsAny<Stream>(),
            It.IsAny<CancellationToken>()))
            .Callback(async (Stream input, Stream output, CancellationToken ct) =>
            {
                input.Seek(0, SeekOrigin.Begin);
                await input.CopyToAsync(output, ct);
                await output.FlushAsync(ct);
            });

        // Act
        var vault = await _sut.LoadAsync(
            info.Id,
            base64Key,
            cancellationTokenSource.Token);

        // Assert
        Assert.Equal(info.Id, vault.Info.Id);
        Assert.Equal(info.Name, name);
        Assert.Equal(info.Description, description);
        Assert.Equal(2, vault.Index.Entries.Count);
        _mockEncryptedCloudStorageProvider.Verify(x => x.GetEncryptedObjectAsync(
            It.Is<string>(y => y == $"{info.Id}/info.json"),
            It.Is<string>(y => y == base64Key),
            It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)), Times.Once);
        _mockEncryptedCloudStorageProvider.Verify(x => x.GetEncryptedObjectAsync(
            It.Is<string>(y => y == $"{info.Id}/index.json"),
            It.Is<string>(y => y == base64Key),
            It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)), Times.Once);
    }

    [Fact]
    public async Task GivenVault_AndBase64Key_WhenDeleteAsync_ThenObjectsListed_AndObjectsDeleted()
    {
        // Arrange
        var name = "Foobar";
        var description = "Description of vault";
        var base64Key = "super secret base64 encoded encryption key";
        var vault = _sut.Create(name, description);
        var cancellationTokenSource = new CancellationTokenSource();
        var objects = new List<string>(["1", "2"]);

        _mockEncryptedCloudStorageProvider.Setup(x => x.ListObjectsAsync(
            It.Is<string>(y => y == $"{vault.Info.Id}/"),
            It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)))
        .ReturnsAsync(objects);

        _mockEncryptedCloudStorageProvider.Setup(x => x.DeleteEncryptedObjectAsync(
            It.Is<string>(y => y == $"1"),
            It.Is<string>(y => y == base64Key),
            It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)))
        .ReturnsAsync(true);

        _mockEncryptedCloudStorageProvider.Setup(x => x.DeleteEncryptedObjectAsync(
            It.Is<string>(y => y == $"2"),
            It.Is<string>(y => y == base64Key),
            It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)))
        .ReturnsAsync(true);

        // Act
        await _sut.DeleteAsync(
            vault,
            base64Key,
            cancellationTokenSource.Token);

        // Assert
        _mockEncryptedCloudStorageProvider.Verify(x => x.ListObjectsAsync(
            It.Is<string>(y => y == $"{vault.Info.Id}/"),
            It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)), Times.Once);
        _mockEncryptedCloudStorageProvider.Verify(x => x.DeleteEncryptedObjectAsync(
            It.Is<string>(y => y == $"1"),
            It.Is<string>(y => y == base64Key),
            It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)), Times.Once);
        _mockEncryptedCloudStorageProvider.Verify(x => x.DeleteEncryptedObjectAsync(
            It.Is<string>(y => y == $"2"),
            It.Is<string>(y => y == base64Key),
            It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)), Times.Once);
    }

    [Fact]
    public async Task GivenVault_AndBase64Key_WhenDeleteAsync_ThenObjectsListed_AndObjectsDeleted_AndFailedToDeleteObject_ThenExceptionThrown()
    {
        // Arrange
        var name = "Foobar";
        var description = "Description of vault";
        var base64Key = "super secret base64 encoded encryption key";
        var vault = _sut.Create(name, description);
        var cancellationTokenSource = new CancellationTokenSource();
        var objects = new List<string>(["1", "2", "3"]);

        _mockEncryptedCloudStorageProvider.Setup(x => x.ListObjectsAsync(
            It.Is<string>(y => y == $"{vault.Info.Id}/"),
            It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)))
        .ReturnsAsync(objects);

        _mockEncryptedCloudStorageProvider.Setup(x => x.DeleteEncryptedObjectAsync(
            It.Is<string>(y => y == $"1"),
            It.Is<string>(y => y == base64Key),
            It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)))
        .ReturnsAsync(true);

        _mockEncryptedCloudStorageProvider.Setup(x => x.DeleteEncryptedObjectAsync(
            It.Is<string>(y => y == $"2"),
            It.Is<string>(y => y == base64Key),
            It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)))
        .ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAnyAsync<CloudStorageProviderException>(async () =>
        {
            await _sut.DeleteAsync(
                vault,
                base64Key,
                cancellationTokenSource.Token);
        });
        _mockEncryptedCloudStorageProvider.Verify(x => x.ListObjectsAsync(
            It.Is<string>(y => y == $"{vault.Info.Id}/"),
            It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)), Times.Once);
        _mockEncryptedCloudStorageProvider.Verify(x => x.DeleteEncryptedObjectAsync(
            It.Is<string>(y => y == $"1"),
            It.Is<string>(y => y == base64Key),
            It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)), Times.Once);
        _mockEncryptedCloudStorageProvider.Verify(x => x.DeleteEncryptedObjectAsync(
            It.Is<string>(y => y == $"2"),
            It.Is<string>(y => y == base64Key),
            It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)), Times.Once);
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
    public async Task GivenVault_WhenVerifyAsync_ThenVaultVerified_AndVerifySuccessful_AndResultsReturned()
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
                    Description = indexEntry!.Description
                };
                secret.UpdateTags([.. indexEntry.Tags!.Split(',')]);
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
                input.Seek(0, SeekOrigin.Begin);
                await input.CopyToAsync(output, ct);
                await output.FlushAsync(ct);
            });

        _mockEncryptedCloudStorageProvider.Setup(x => x.ListObjectsAsync(
            $"{info.Id}/secrets/",
            CancellationToken.None))
            .ReturnsAsync([.. entries.Select(x => $"{info.Id}/secrets/{x.Id}")]);

        // Act
        var results = await _sut.VerifyAsync(
            vault.Object,
            base64Key,
            CancellationToken.None);

        // Assert
        Assert.True(results.Success);
    }

    [Fact]
    public async Task GivenVault_WhenVerifyAsync_ThenVaultVerified_AndTagsMismatch_AndResultsReturned()
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
                    Description = indexEntry!.Description
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
                input.Seek(0, SeekOrigin.Begin);
                await input.CopyToAsync(output, ct);
                await output.FlushAsync(ct);
            });

        _mockEncryptedCloudStorageProvider.Setup(x => x.ListObjectsAsync(
            $"{info.Id}/secrets/",
            CancellationToken.None))
            .ReturnsAsync([.. entries.Select(x => $"{info.Id}/secrets/{x.Id}")]);

        // Act
        var results = await _sut.VerifyAsync(
            vault.Object,
            base64Key,
            CancellationToken.None);

        // Assert
        Assert.False(results.Success);
        Assert.Equal(2, results.MismatchedSecrets);
    }

    [Fact]
    public async Task GivenVault_WhenVerifyAsync_ThenVaultVerified_AndMissingSecrets_AndResultsReturned()
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
                return null;
            });

        _mockEncryptedCloudStorageProvider.Setup(x => x.ListObjectsAsync(
            $"{info.Id}/secrets/",
            CancellationToken.None))
            .ReturnsAsync([.. entries.Select(x => $"{info.Id}/secrets/{x.Id}")]);

        // Act
        var results = await _sut.VerifyAsync(
            vault.Object,
            base64Key,
            CancellationToken.None);

        // Assert
        Assert.False(results.Success);
        Assert.Equal(2, results.MissingSecrets);
    }

    [Fact]
    public async Task GivenVault_WhenVerifyAsync_ThenVaultVerified_AndUnindexedSecrets_AndResultsReturned()
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
        var index = new VaultIndex();

        vault.SetupGet(x => x.Info)
            .Returns(info);

        vault.SetupGet(x => x.Index)
            .Returns(index);

        _mockEncryptedCloudStorageProvider.Setup(x => x.ListObjectsAsync(
            $"{info.Id}/secrets/",
            CancellationToken.None))
            .ReturnsAsync([.. entries.Select(x => $"{info.Id}/secrets/{x.Id}")]);

        // Act
        var results = await _sut.VerifyAsync(
            vault.Object,
            base64Key,
            CancellationToken.None);

        // Assert
        Assert.False(results.Success);
        Assert.Equal(2, results.UnindexedSecrets.Count);
        Assert.DoesNotContain(entries, x => !results.UnindexedSecrets.Contains(x.Id));
    }
}
