using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.S3.Model;
using clypse.core.Cloud.Exceptions;
using clypse.core.Cloud.Interfaces;
using clypse.core.Compression.Interfaces;
using clypse.core.Secrets;
using clypse.core.Vault;
using clypse.core.Vault.Exceptions;
using Microsoft.VisualBasic;
using Moq;

namespace clypse.core.UnitTests.Vault;

public class VaultManagerTests
{
    private readonly JsonSerializerOptions jsonSerializerOptions = new ()
    {
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
        },
    };

    private readonly Mock<ICompressionService> mockCompressionService;
    private readonly Mock<ICloudStorageProvider> mockInnerCloudStorageProvider;
    private readonly Mock<IEncryptedCloudStorageProvider> mockEncryptedCloudStorageProvider;
    private readonly VaultManager sut;

    public VaultManagerTests()
    {
        this.mockCompressionService = new Mock<ICompressionService>();
        this.mockInnerCloudStorageProvider = new Mock<ICloudStorageProvider>();
        this.mockEncryptedCloudStorageProvider = new Mock<IEncryptedCloudStorageProvider>();

        this.mockEncryptedCloudStorageProvider.SetupGet(
            x => x.InnerProvider)
            .Returns(this.mockInnerCloudStorageProvider.Object);

        this.sut = new VaultManager(
            "foobar",
            this.mockCompressionService.Object,
            this.mockEncryptedCloudStorageProvider.Object);
    }

    [Fact]
    public void GivenName_AndDescription_WhenCreate_ThenVaultCreated()
    {
        // Arrange
        var name = "Foobar";
        var description = "Description of vault";

        // Act
        var vault = this.sut.Create(name, description);

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
        var vault = this.sut.Create(name, description);
        var cancellationTokenSource = new CancellationTokenSource();

        // Act
        await this.sut.SaveAsync(
            vault,
            base64Key,
            null,
            cancellationTokenSource.Token);

        // Assert
        this.mockEncryptedCloudStorageProvider.Verify(
            x => x.PutEncryptedObjectAsync(
            It.Is<string>(y => y == $"foobar/{vault.Info.Id}/info.json"),
            It.IsAny<Stream>(),
            It.Is<string>(y => y == base64Key),
            It.IsAny<MetadataCollection?>(),
            It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)), Times.Once);
        this.mockEncryptedCloudStorageProvider.Verify(
            x => x.PutEncryptedObjectAsync(
            It.Is<string>(y => y == $"foobar/{vault.Info.Id}/index.json"),
            It.IsAny<Stream>(),
            It.Is<string>(y => y == base64Key),
            It.IsAny<MetadataCollection?>(),
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
        var results = await this.sut.SaveAsync(
            mockVault.Object,
            base64Key,
            null,
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
                        string.Empty),
                    new VaultIndexEntry(
                        "2",
                        "Foo",
                        "Bar",
                        string.Empty),
                ],
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
        var results = await this.sut.SaveAsync(
            mockVault.Object,
            base64Key,
            null,
            cancellationTokenSource.Token);

        // Assert
        Assert.True(results.Success);
        Assert.Empty(index.Entries);
        mockVault.Verify(x => x.MakeClean(), Times.Once);
        this.mockEncryptedCloudStorageProvider.Verify(
            x => x.PutEncryptedObjectAsync(
            It.Is<string>(y => y == $"foobar/{info.Id}/info.json"),
            It.IsAny<Stream>(),
            It.Is<string>(y => y == base64Key),
            It.IsAny<MetadataCollection?>(),
            It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)), Times.Once);
        this.mockEncryptedCloudStorageProvider.Verify(
            x => x.DeleteEncryptedObjectAsync(
            It.Is<string>(y => y == $"foobar/{info.Id}/secrets/1"),
            It.Is<string>(y => y == base64Key),
            It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)), Times.Once);
        this.mockEncryptedCloudStorageProvider.Verify(
            x => x.DeleteEncryptedObjectAsync(
            It.Is<string>(y => y == $"foobar/{info.Id}/secrets/2"),
            It.Is<string>(y => y == base64Key),
            It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)), Times.Once);
        this.mockEncryptedCloudStorageProvider.Verify(
            x => x.PutEncryptedObjectAsync(
            It.Is<string>(y => y == $"foobar/{info.Id}/index.json"),
            It.IsAny<Stream>(),
            It.Is<string>(y => y == base64Key),
            It.IsAny<MetadataCollection?>(),
            It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)), Times.Once);
    }

    [Fact]
    public async Task GivenId_AndBase64Key_WhenLoadAsync_ThenInfoLoaded_AndIndexLoaded_AndVaultReturned()
    {
        // Arrange
        var name = "Foobar";
        var description = "Description of vault";
        var base64Key = "super secret base64 encoded encryption key";
        var manifest = new VaultManifest();
        var info = new VaultInfo(name, description);
        var index = new VaultIndex
        {
            Entries =
                [
                    new VaultIndexEntry(
                        "1",
                        "Foo",
                        "Bar",
                        string.Empty),
                    new VaultIndexEntry(
                        "2",
                        "Foo",
                        "Bar",
                        string.Empty),
                ],
        };
        var cancellationTokenSource = new CancellationTokenSource();

        this.mockInnerCloudStorageProvider.Setup(
            x => x.GetObjectAsync(
                It.Is<string>(y => y == $"foobar/{info.Id}/manifest.json"),
                It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)))
            .Returns(async (string key, CancellationToken ct) =>
            {
                var output = new MemoryStream();
                await JsonSerializer.SerializeAsync(output, manifest, this.jsonSerializerOptions, ct);
                output.Seek(0, SeekOrigin.Begin);
                return output;
            });

        this.mockEncryptedCloudStorageProvider.Setup(
            x => x.GetEncryptedObjectAsync(
            It.Is<string>(y => y == $"foobar/{info.Id}/info.json"),
            It.Is<string>(y => y == base64Key),
            It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)))
            .Returns(async (string key, string base64EncryptionKey, CancellationToken ct) =>
            {
                var output = new MemoryStream();
                await JsonSerializer.SerializeAsync(output, info, this.jsonSerializerOptions, ct);
                return output;
            });

        this.mockEncryptedCloudStorageProvider.Setup(
            x => x.GetEncryptedObjectAsync(
            It.Is<string>(y => y == $"foobar/{info.Id}/index.json"),
            It.Is<string>(y => y == base64Key),
            It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)))
            .Returns(async (string key, string base64EncryptionKey, CancellationToken ct) =>
            {
                var output = new MemoryStream();
                await JsonSerializer.SerializeAsync(output, index, this.jsonSerializerOptions, ct);
                return output;
            });

        this.mockCompressionService.Setup(
            x => x.DecompressAsync(
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
        var vault = await this.sut.LoadAsync(
            info.Id,
            base64Key,
            cancellationTokenSource.Token);

        // Assert
        Assert.Equal(info.Id, vault.Info.Id);
        Assert.Equal(info.Name, name);
        Assert.Equal(info.Description, description);
        Assert.Equal(2, vault.Index.Entries.Count);
        this.mockEncryptedCloudStorageProvider.Verify(
            x => x.GetEncryptedObjectAsync(
            It.Is<string>(y => y == $"foobar/{info.Id}/info.json"),
            It.Is<string>(y => y == base64Key),
            It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)), Times.Once);
        this.mockEncryptedCloudStorageProvider.Verify(
            x => x.GetEncryptedObjectAsync(
            It.Is<string>(y => y == $"foobar/{info.Id}/index.json"),
            It.Is<string>(y => y == base64Key),
            It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)), Times.Once);
    }

    [Fact]
    public async Task GivenId_AndBase64Key_WhenLoadAsync_ThenInfoNotFound_AndIndexLoaded_AndExceptionThrown()
    {
        // Arrange
        var name = "Foobar";
        var description = "Description of vault";
        var base64Key = "super secret base64 encoded encryption key";
        var manifest = new VaultManifest();
        var info = new VaultInfo(name, description);
        var index = new VaultIndex
        {
            Entries =
                [
                    new VaultIndexEntry(
                        "1",
                        "Foo",
                        "Bar",
                        string.Empty),
                    new VaultIndexEntry(
                        "2",
                        "Foo",
                        "Bar",
                        string.Empty),
                ],
        };
        var cancellationTokenSource = new CancellationTokenSource();

        this.mockInnerCloudStorageProvider.Setup(
            x => x.GetObjectAsync(
                It.Is<string>(y => y == $"foobar/{info.Id}/manifest.json"),
                It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)))
            .Returns(async (string key, CancellationToken ct) =>
            {
                var output = new MemoryStream();
                await JsonSerializer.SerializeAsync(output, manifest, this.jsonSerializerOptions, ct);
                output.Seek(0, SeekOrigin.Begin);
                return output;
            });

        this.mockEncryptedCloudStorageProvider.Setup(x => x.GetEncryptedObjectAsync(
            It.Is<string>(y => y == $"{info.Id}/info.json"),
            It.Is<string>(y => y == base64Key),
            It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)))
            .Returns(async () =>
            {
                await Task.Yield();
                return null;
            });

        // Act & Assert
        await Assert.ThrowsAnyAsync<FailedToLoadVaultInfoException>(async () =>
        {
            _ = await this.sut.LoadAsync(
                info.Id,
                base64Key,
                cancellationTokenSource.Token);
        });
        this.mockEncryptedCloudStorageProvider.Verify(
            x => x.GetEncryptedObjectAsync(
            It.Is<string>(y => y == $"foobar/{info.Id}/info.json"),
            It.Is<string>(y => y == base64Key),
            It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)), Times.Once);
    }

    [Fact]
    public async Task GivenId_AndBase64Key_WhenLoadAsync_ThenInfoLoaded_AndIndexNotFound_AndExceptionThrown()
    {
        // Arrange
        var name = "Foobar";
        var description = "Description of vault";
        var base64Key = "super secret base64 encoded encryption key";
        var manifest = new VaultManifest();
        var info = new VaultInfo(name, description);
        var index = new VaultIndex
        {
            Entries =
                [
                    new VaultIndexEntry(
                        "1",
                        "Foo",
                        "Bar",
                        string.Empty),
                    new VaultIndexEntry(
                        "2",
                        "Foo",
                        "Bar",
                        string.Empty),
                ],
        };
        var cancellationTokenSource = new CancellationTokenSource();

        this.mockInnerCloudStorageProvider.Setup(
            x => x.GetObjectAsync(
                It.Is<string>(y => y == $"foobar/{info.Id}/manifest.json"),
                It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)))
            .Returns(async (string key, CancellationToken ct) =>
            {
                var output = new MemoryStream();
                await JsonSerializer.SerializeAsync(output, manifest, this.jsonSerializerOptions, ct);
                output.Seek(0, SeekOrigin.Begin);
                return output;
            });

        this.mockEncryptedCloudStorageProvider.Setup(
            x => x.GetEncryptedObjectAsync(
            It.Is<string>(y => y == $"foobar/{info.Id}/info.json"),
            It.Is<string>(y => y == base64Key),
            It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)))
            .Returns(async (string key, string base64EncryptionKey, CancellationToken ct) =>
            {
                var output = new MemoryStream();
                await JsonSerializer.SerializeAsync(output, info, this.jsonSerializerOptions, ct);
                return output;
            });

        this.mockEncryptedCloudStorageProvider.Setup(
            x => x.GetEncryptedObjectAsync(
            It.Is<string>(y => y == $"foobar/{info.Id}/index.json"),
            It.Is<string>(y => y == base64Key),
            It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)))
            .Returns(async (string key, string base64EncryptionKey, CancellationToken ct) =>
            {
                await Task.Yield();
                return null;
            });

        this.mockCompressionService.Setup(
            x => x.DecompressAsync(
            It.IsAny<Stream>(),
            It.IsAny<Stream>(),
            It.IsAny<CancellationToken>()))
            .Callback(async (Stream input, Stream output, CancellationToken ct) =>
            {
                input.Seek(0, SeekOrigin.Begin);
                await input.CopyToAsync(output, ct);
                await output.FlushAsync(ct);
            });

        // Act & Assert
        await Assert.ThrowsAnyAsync<FailedToLoadVaultIndexException>(async () =>
        {
            _ = await this.sut.LoadAsync(
                info.Id,
                base64Key,
                cancellationTokenSource.Token);
        });
        this.mockEncryptedCloudStorageProvider.Verify(
            x => x.GetEncryptedObjectAsync(
            It.Is<string>(y => y == $"foobar/{info.Id}/info.json"),
            It.Is<string>(y => y == base64Key),
            It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)), Times.Once);
        this.mockEncryptedCloudStorageProvider.Verify(
            x => x.GetEncryptedObjectAsync(
            It.Is<string>(y => y == $"foobar/{info.Id}/index.json"),
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
        var vault = this.sut.Create(name, description);
        var cancellationTokenSource = new CancellationTokenSource();
        var objects = new List<string>(["1", "2"]);
        var info = new VaultInfo(name, description);

        this.mockEncryptedCloudStorageProvider.Setup(
            x => x.GetEncryptedObjectAsync(
            It.Is<string>(y => y == $"foobar/{vault.Info.Id}/info.json"),
            It.Is<string>(y => y == base64Key),
            It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)))
            .Returns(async (string key, string base64EncryptionKey, CancellationToken ct) =>
            {
                var output = new MemoryStream();
                await JsonSerializer.SerializeAsync(output, info, this.jsonSerializerOptions, ct);
                return output;
            });

        this.mockEncryptedCloudStorageProvider.Setup(
            x => x.ListObjectsAsync(
            It.Is<string>(y => y == $"foobar/{vault.Info.Id}/"),
            It.Is<string?>(y => y == null),
            It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)))
        .ReturnsAsync(objects);

        this.mockEncryptedCloudStorageProvider.Setup(
            x => x.DeleteEncryptedObjectAsync(
            It.Is<string>(y => y == $"1"),
            It.Is<string>(y => y == base64Key),
            It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)))
        .ReturnsAsync(true);

        this.mockEncryptedCloudStorageProvider.Setup(
            x => x.DeleteEncryptedObjectAsync(
            It.Is<string>(y => y == $"2"),
            It.Is<string>(y => y == base64Key),
            It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)))
        .ReturnsAsync(true);

        this.mockCompressionService.Setup(
            x => x.DecompressAsync(
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
        await this.sut.DeleteAsync(
            vault,
            base64Key,
            cancellationTokenSource.Token);

        // Assert
        this.mockEncryptedCloudStorageProvider.Verify(
            x => x.ListObjectsAsync(
            It.Is<string>(y => y == $"foobar/{vault.Info.Id}/"),
            It.IsAny<string?>(),
            It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)), Times.Once);
        this.mockEncryptedCloudStorageProvider.Verify(
            x => x.DeleteEncryptedObjectAsync(
            It.Is<string>(y => y == $"1"),
            It.Is<string>(y => y == base64Key),
            It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)), Times.Once);
        this.mockEncryptedCloudStorageProvider.Verify(
            x => x.DeleteEncryptedObjectAsync(
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
        var vault = this.sut.Create(name, description);
        var cancellationTokenSource = new CancellationTokenSource();
        var objects = new List<string>(["1", "2", "3"]);
        var info = new VaultInfo(name, description);

        this.mockEncryptedCloudStorageProvider.Setup(
            x => x.GetEncryptedObjectAsync(
            It.Is<string>(y => y == $"foobar/{vault.Info.Id}/info.json"),
            It.Is<string>(y => y == base64Key),
            It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)))
            .Returns(async (string key, string base64EncryptionKey, CancellationToken ct) =>
            {
                var output = new MemoryStream();
                await JsonSerializer.SerializeAsync(output, info, this.jsonSerializerOptions, ct);
                return output;
            });

        this.mockEncryptedCloudStorageProvider.Setup(
            x => x.ListObjectsAsync(
            It.Is<string>(y => y == $"foobar/{vault.Info.Id}/"),
            It.Is<string?>(y => y == null),
            It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)))
        .ReturnsAsync(objects);

        this.mockEncryptedCloudStorageProvider.Setup(
            x => x.DeleteEncryptedObjectAsync(
            It.Is<string>(y => y == $"1"),
            It.Is<string>(y => y == base64Key),
            It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)))
        .ReturnsAsync(true);

        this.mockEncryptedCloudStorageProvider.Setup(
            x => x.DeleteEncryptedObjectAsync(
            It.Is<string>(y => y == $"2"),
            It.Is<string>(y => y == base64Key),
            It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)))
        .ReturnsAsync(true);

        this.mockCompressionService.Setup(
            x => x.DecompressAsync(
            It.IsAny<Stream>(),
            It.IsAny<Stream>(),
            It.IsAny<CancellationToken>()))
            .Callback(async (Stream input, Stream output, CancellationToken ct) =>
            {
                input.Seek(0, SeekOrigin.Begin);
                await input.CopyToAsync(output, ct);
                await output.FlushAsync(ct);
            });

        // Act & Assert
        await Assert.ThrowsAnyAsync<CloudStorageProviderException>(async () =>
        {
            await this.sut.DeleteAsync(
                vault,
                base64Key,
                cancellationTokenSource.Token);
        });
        this.mockEncryptedCloudStorageProvider.Verify(
            x => x.ListObjectsAsync(
            It.Is<string>(y => y == $"foobar/{vault.Info.Id}/"),
            It.Is<string?>(y => y == null),
            It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)), Times.Once);
        this.mockEncryptedCloudStorageProvider.Verify(
            x => x.DeleteEncryptedObjectAsync(
            It.Is<string>(y => y == $"1"),
            It.Is<string>(y => y == base64Key),
            It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)), Times.Once);
        this.mockEncryptedCloudStorageProvider.Verify(
            x => x.DeleteEncryptedObjectAsync(
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
        var manifest = new VaultManifest();
        var vault = this.sut.Create(name, description);
        var cancellationTokenSource = new CancellationTokenSource();

        this.mockInnerCloudStorageProvider.Setup(
            x => x.GetObjectAsync(
                It.Is<string>(y => y == $"foobar/{vault.Info.Id}/manifest.json"),
                It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)))
            .Returns(async (string key, CancellationToken ct) =>
            {
                var output = new MemoryStream();
                await JsonSerializer.SerializeAsync(output, manifest, this.jsonSerializerOptions, ct);
                output.Seek(0, SeekOrigin.Begin);
                return output;
            });

        var secret1 = new Secret
        {
            Name = "Secret1",
            Description = description,
        };
        vault.AddSecret(secret1);
        await this.sut.SaveAsync(
            vault,
            base64Key,
            null,
            cancellationTokenSource.Token);

        this.mockEncryptedCloudStorageProvider.Reset();

        this.mockEncryptedCloudStorageProvider.SetupGet(
            x => x.InnerProvider)
            .Returns(this.mockInnerCloudStorageProvider.Object);

        // Act
        secret1.Description = updatedDescription;
        vault.UpdateSecret(secret1);
        var results = await this.sut.SaveAsync(
            vault,
            base64Key,
            null,
            cancellationTokenSource.Token);

        // Assert
        Assert.True(results.Success);
        Assert.Equal(0, results.SecretsCreated);
        Assert.Equal(1, results.SecretsUpdated);
        Assert.Equal(0, results.SecretsDeleted);
        this.mockEncryptedCloudStorageProvider.Verify(
            x => x.PutEncryptedObjectAsync(
            It.Is<string>(y => y == $"foobar/{vault.Info.Id}/info.json"),
            It.IsAny<Stream>(),
            It.Is<string>(y => y == base64Key),
            It.IsAny<MetadataCollection?>(),
            It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)), Times.Once);
        this.mockEncryptedCloudStorageProvider.Verify(
            x => x.PutEncryptedObjectAsync(
            It.Is<string>(y => y == $"foobar/{vault.Info.Id}/secrets/{secret1.Id}"),
            It.IsAny<Stream>(),
            It.Is<string>(y => y == base64Key),
            It.IsAny<MetadataCollection?>(),
            It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)), Times.Once);
        this.mockEncryptedCloudStorageProvider.Verify(
            x => x.PutEncryptedObjectAsync(
            It.Is<string>(y => y == $"foobar/{vault.Info.Id}/index.json"),
            It.IsAny<Stream>(),
            It.Is<string>(y => y == base64Key),
            It.IsAny<MetadataCollection?>(),
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
            new ("1",
                "Secret1",
                "Description of Secret1.",
                "apple,orange,pear"),
            new ("2",
                "Secret2",
                "Description of Secret2.",
                "red,green,blue"),
        };
        var info = new VaultInfo(
            name,
            description);
        var index = new VaultIndex
        {
            Entries = entries,
        };

        vault.SetupGet(x => x.Info)
            .Returns(info);

        vault.SetupGet(x => x.Index)
            .Returns(index);

        this.mockEncryptedCloudStorageProvider.Setup(
            x => x.GetEncryptedObjectAsync(
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
                secret.UpdateTags([.. indexEntry.Tags!.Split(',')]);
                var stream = new MemoryStream();
                JsonSerializer.Serialize(stream, secret, this.jsonSerializerOptions);
                return stream;
            });

        this.mockCompressionService.Setup(
            x => x.DecompressAsync(
            It.IsAny<Stream>(),
            It.IsAny<Stream>(),
            It.IsAny<CancellationToken>()))
            .Callback(async (Stream input, Stream output, CancellationToken ct) =>
            {
                input.Seek(0, SeekOrigin.Begin);
                await input.CopyToAsync(output, ct);
                await output.FlushAsync(ct);
            });

        this.mockEncryptedCloudStorageProvider.Setup(
            x => x.ListObjectsAsync(
            $"foobar/{info.Id}/secrets/",
            null,
            CancellationToken.None))
            .ReturnsAsync([.. entries.Select(x => $"foobar/{info.Id}/secrets/{x.Id}")]);

        // Act
        var results = await this.sut.VerifyAsync(
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
            new ("1",
                "Secret1",
                "Description of Secret1.",
                "apple,orange,pear"),
            new ("2",
                "Secret2",
                "Description of Secret2.",
                "red,green,blue"),
        };
        var info = new VaultInfo(
            name,
            description);
        var index = new VaultIndex
        {
            Entries = entries,
        };

        vault.SetupGet(x => x.Info)
            .Returns(info);

        vault.SetupGet(x => x.Index)
            .Returns(index);

        this.mockEncryptedCloudStorageProvider.Setup(
            x => x.GetEncryptedObjectAsync(
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
                JsonSerializer.Serialize(stream, secret, this.jsonSerializerOptions);
                return stream;
            });

        this.mockCompressionService.Setup(x =>
        x.DecompressAsync(
            It.IsAny<Stream>(),
            It.IsAny<Stream>(),
            It.IsAny<CancellationToken>()))
            .Callback(async (Stream input, Stream output, CancellationToken ct) =>
            {
                input.Seek(0, SeekOrigin.Begin);
                await input.CopyToAsync(output, ct);
                await output.FlushAsync(ct);
            });

        this.mockEncryptedCloudStorageProvider.Setup(
            x => x.ListObjectsAsync(
            $"foobar/{info.Id}/secrets/",
            null,
            CancellationToken.None))
            .ReturnsAsync([.. entries.Select(x => $"foobar/{info.Id}/secrets/{x.Id}")]);

        // Act
        var results = await this.sut.VerifyAsync(
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
            new ("1",
                "Secret1",
                "Description of Secret1.",
                "apple,orange,pear"),
            new ("2",
                "Secret2",
                "Description of Secret2.",
                "red,green,blue"),
        };
        var info = new VaultInfo(
            name,
            description);
        var index = new VaultIndex
        {
            Entries = entries,
        };

        vault.SetupGet(x => x.Info)
            .Returns(info);

        vault.SetupGet(x => x.Index)
            .Returns(index);

        this.mockEncryptedCloudStorageProvider.Setup(
            x => x.GetEncryptedObjectAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((string key, string base64EncryptionKey, CancellationToken ct) =>
            {
                return null;
            });

        this.mockEncryptedCloudStorageProvider.Setup(
            x => x.ListObjectsAsync(
            $"foobar/{info.Id}/secrets/",
            null,
            CancellationToken.None))
            .ReturnsAsync([.. entries.Select(x => $"foobar/{info.Id}/secrets/{x.Id}")]);

        // Act
        var results = await this.sut.VerifyAsync(
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
            new ("1",
                "Secret1",
                "Description of Secret1.",
                "apple,orange,pear"),
            new ("2",
                "Secret2",
                "Description of Secret2.",
                "red,green,blue"),
        };
        var info = new VaultInfo(
            name,
            description);
        var index = new VaultIndex();

        vault.SetupGet(x => x.Info)
            .Returns(info);

        vault.SetupGet(x => x.Index)
            .Returns(index);

        this.mockEncryptedCloudStorageProvider.Setup(
            x => x.ListObjectsAsync(
            $"foobar/{info.Id}/secrets/",
            null,
            CancellationToken.None))
            .ReturnsAsync([.. entries.Select(x => $"foobar/{info.Id}/secrets/{x.Id}")]);

        // Act
        var results = await this.sut.VerifyAsync(
            vault.Object,
            base64Key,
            CancellationToken.None);

        // Assert
        Assert.False(results.Success);
        Assert.Equal(2, results.UnindexedSecrets.Count);
        Assert.DoesNotContain(entries, x => !results.UnindexedSecrets.Contains(x.Id));
    }

    [Fact]
    public async Task GivenVault_AndSecretId_AndBase64Key_WhenGetSecretAsync_Then()
    {
        // Arrange
        var name = "Foobar";
        var description = "Description of vault";
        var base64Key = "super secret base64 encoded encryption key";
        var vault = this.sut.Create(name, description);
        var secretId = "1";
        var cancellationTokenSource = new CancellationTokenSource();

        var retrievedSecret = new Secret
        {
            Id = secretId,
            Name = name,
            Description = description,
        };

        this.mockEncryptedCloudStorageProvider.Setup(
            x => x.GetEncryptedObjectAsync(
            It.Is<string>(y => y == $"foobar/{vault.Info.Id}/secrets/{secretId}"),
            It.Is<string>(y => y == base64Key),
            It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)))
            .ReturnsAsync(() =>
            {
                var stream = new MemoryStream();
                JsonSerializer.Serialize(stream, retrievedSecret, this.jsonSerializerOptions);
                return stream;
            });

        this.mockCompressionService.Setup(
            x => x.DecompressAsync(
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
        var secret = await this.sut.GetSecretAsync(
            vault,
            secretId,
            base64Key,
            cancellationTokenSource.Token);

        // Assert
        Assert.NotNull(secret);
        Assert.Equal(Enums.SecretType.None, secret.SecretType);
        Assert.Equal(name, secret.Name);
        Assert.Equal(description, secret.Description);
    }

    [Fact]
    public async Task GivenVault_AndWebSecretId_AndBase64Key_WhenGetSecretAsync_Then()
    {
        // Arrange
        var name = "Foobar";
        var description = "Description of vault";
        var base64Key = "super secret base64 encoded encryption key";
        var vault = this.sut.Create(name, description);
        var secretId = "1";
        var cancellationTokenSource = new CancellationTokenSource();

        var retrievedSecret = new WebSecret
        {
            Id = secretId,
            Name = name,
            Description = description,
        };

        this.mockEncryptedCloudStorageProvider.Setup(
            x => x.GetEncryptedObjectAsync(
            It.Is<string>(y => y == $"foobar/{vault.Info.Id}/secrets/{secretId}"),
            It.Is<string>(y => y == base64Key),
            It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)))
            .ReturnsAsync(() =>
            {
                var stream = new MemoryStream();
                JsonSerializer.Serialize(stream, retrievedSecret, this.jsonSerializerOptions);
                return stream;
            });

        this.mockCompressionService.Setup(x => x.DecompressAsync(
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
        var secret = await this.sut.GetSecretAsync(
            vault,
            secretId,
            base64Key,
            cancellationTokenSource.Token);

        // Assert
        Assert.NotNull(secret);
        Assert.Equal(Enums.SecretType.Web, secret.SecretType);
        Assert.Equal(name, secret.Name);
        Assert.Equal(description, secret.Description);
    }

    [Fact]
    public async Task GivenCancellationToken_WhenListVaultIdsAsync_ThenObjectsListedWithDelimiter_AndObjectsReturned()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();

        var ids = new List<string>
        {
            "Foo",
            "Bar",
        };

        this.mockEncryptedCloudStorageProvider.Setup(x => x.ListObjectsAsync(
            It.Is<string>(y => y == "foobar/"),
            It.Is<string>(y => y == "/"),
            It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)))
            .ReturnsAsync(ids);

        // Act
        var vaultIds = await this.sut.ListVaultIdsAsync(cancellationTokenSource.Token);

        // Assert
        Assert.Equal(ids.Count, vaultIds.Count);
        Assert.Equal(ids[0], vaultIds[0]);
        Assert.Equal(ids[1], vaultIds[1]);
    }
}
