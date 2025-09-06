using clypse.core.Cloud;
using clypse.core.Cloud.Aws.S3;
using clypse.core.Cloud.Interfaces;
using clypse.core.Compression;
using clypse.core.Cryptogtaphy;
using clypse.core.Cryptogtaphy.Interfaces;
using clypse.core.Vault;
using clypse.core.Vault.Exceptions;
using Moq;

namespace clypse.core.UnitTests.Vault;

public class AwsS3VaultManagerBootstrapperServiceTests
{
    private readonly Mock<ICloudStorageProvider> mockCloudStorageProvider;
    private readonly Mock<IAwsEncryptedCloudStorageProviderTransformer> mockAwsEncryptedCloudStorageProviderTransformer;
    private readonly Mock<ICryptoService> mockCryptoService;
    private AwsS3VaultManagerBootstrapperService sut;

    private string prefix = "foobar";

    public AwsS3VaultManagerBootstrapperServiceTests()
    {
        this.mockCloudStorageProvider = new Mock<ICloudStorageProvider>();
        this.mockAwsEncryptedCloudStorageProviderTransformer = new Mock<IAwsEncryptedCloudStorageProviderTransformer>();
        this.mockCryptoService = new Mock<ICryptoService>();
        this.sut = new AwsS3VaultManagerBootstrapperService(
            this.prefix,
            new TestAwsCloudStorageProvider(
                this.mockCloudStorageProvider,
                this.mockAwsEncryptedCloudStorageProviderTransformer));
    }

    [Fact]
    public async Task GivenId_AndInvalidTestAwsCloudStorageProvider_WhenCreateVaultManagerForVaultAsync_ThenExceptionThrown()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        var id = Guid.NewGuid().ToString();
        var manifest = new VaultManifest
        {
            ClypseCoreVersion = "1.0.0",
            CompressionServiceName = "GZipCompressionService",
            CryptoServiceName = "BouncyCastleAesGcmCryptoService",
            EncryptedCloudStorageProviderName = "AwsS3E2eCloudStorageProvider",
            Parameters = [],
        };

        var defaultKeyDerivationOptions = KeyDerivationServiceDefaultOptions.Blazor_Argon2id();
        foreach (var curParam in defaultKeyDerivationOptions.Parameters)
        {
            manifest.Parameters[$"KeyDerivationService_{curParam.Key}"] = curParam.Value;
        }

        this.sut = new AwsS3VaultManagerBootstrapperService(
            this.prefix,
            new InvalidTestAwsCloudStorageProvider(
                this.mockCloudStorageProvider));

        // Act & Assert
        await Assert.ThrowsAnyAsync<CloudStorageProviderDoesNotImplementIAwsEncryptedCloudStorageProviderTransformerException>(async () =>
        {
            await this.sut.CreateVaultManagerForVaultAsync(id, cancellationTokenSource.Token);
        });
    }

    [Fact]
    public async Task GivenId_AndManifestContainsGZipCompression_AndManifestContainsBouncyCastleAesGcmCrypto_AndManifestContiansAwsS3E2eProvider_AndCancellationToken_WhenCreateVaultManagerForVaultAsync_ThenManifestLoaded_AndVaultManagerCreated()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        var id = Guid.NewGuid().ToString();
        var manifest = new VaultManifest
        {
            ClypseCoreVersion = "1.0.0",
            CompressionServiceName = "GZipCompressionService",
            CryptoServiceName = "BouncyCastleAesGcmCryptoService",
            EncryptedCloudStorageProviderName = "AwsS3E2eCloudStorageProvider",
            Parameters = [],
        };

        var defaultKeyDerivationOptions = KeyDerivationServiceDefaultOptions.Blazor_Argon2id();
        foreach (var curParam in defaultKeyDerivationOptions.Parameters)
        {
            manifest.Parameters[$"KeyDerivationService_{curParam.Key}"] = curParam.Value;
        }

        this.mockCloudStorageProvider.Setup(x => x.GetObjectAsync(
            It.Is<string>(y => y == $"{this.prefix}/{id}/manifest.json"),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                var json = System.Text.Json.JsonSerializer.Serialize(manifest);
                var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
                stream.Seek(0, SeekOrigin.Begin);
                return stream;
            });

        this.mockAwsEncryptedCloudStorageProviderTransformer.Setup(
            x => x.CreateE2eProvider(It.IsAny<ICryptoService>()))
            .Returns(new AwsS3E2eCloudStorageProvider(
                "foo",
                Mock.Of<IAmazonS3Client>(),
                Mock.Of<ICryptoService>()));

        // Act
        var result = await this.sut.CreateVaultManagerForVaultAsync(id, cancellationTokenSource.Token) as VaultManager;

        // Assert
        Assert.NotNull(result);
        Assert.IsType<VaultManager>(result);
        Assert.IsType<KeyDerivationService>(result.KeyDerivationService);
        Assert.IsType<GZipCompressionService>(result.CompressionService);
        Assert.IsType<AwsS3E2eCloudStorageProvider>(result.EncryptedCloudStorageProvider);
    }

    [Fact]
    public async Task GivenId_AndManifestContainsGZipCompression_AndManifestContainsBouncyCastleAesGcmCrypto_AndManifestContiansAwsS3SseProvider_AndCancellationToken_WhenCreateVaultManagerForVaultAsync_ThenManifestLoaded_AndVaultManagerCreated()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        var id = Guid.NewGuid().ToString();
        var manifest = new VaultManifest
        {
            ClypseCoreVersion = "1.0.0",
            CompressionServiceName = "GZipCompressionService",
            CryptoServiceName = "NativeAesGcmCryptoService",
            EncryptedCloudStorageProviderName = "AwsS3SseCloudStorageProvider",
            Parameters = [],
        };

        var defaultKeyDerivationOptions = KeyDerivationServiceDefaultOptions.Blazor_Argon2id();
        foreach (var curParam in defaultKeyDerivationOptions.Parameters)
        {
            manifest.Parameters[$"KeyDerivationService_{curParam.Key}"] = curParam.Value;
        }

        this.mockCloudStorageProvider.Setup(x => x.GetObjectAsync(
            It.Is<string>(y => y == $"{this.prefix}/{id}/manifest.json"),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                var json = System.Text.Json.JsonSerializer.Serialize(manifest);
                var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
                stream.Seek(0, SeekOrigin.Begin);
                return stream;
            });

        this.mockAwsEncryptedCloudStorageProviderTransformer.Setup(
            x => x.CreateSseProvider())
            .Returns(new AwsS3SseCCloudStorageProvider(
                "foo",
                Mock.Of<IAmazonS3Client>()));

        // Act
        var result = await this.sut.CreateVaultManagerForVaultAsync(id, cancellationTokenSource.Token) as VaultManager;

        // Assert
        Assert.NotNull(result);
        Assert.IsType<VaultManager>(result);
        Assert.IsType<KeyDerivationService>(result.KeyDerivationService);
        Assert.IsType<GZipCompressionService>(result.CompressionService);
        Assert.IsType<AwsS3SseCCloudStorageProvider>(result.EncryptedCloudStorageProvider);
    }

    [Fact]
    public async Task GivenId_AndManifestContainsUnsupportedCompression_WhenCreateVaultManagerForVaultAsync_ThenExceptionThrown()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        var id = Guid.NewGuid().ToString();
        var manifest = new VaultManifest
        {
            ClypseCoreVersion = "1.0.0",
            CompressionServiceName = "Foobar",
            CryptoServiceName = "NativeAesGcmCryptoService",
            EncryptedCloudStorageProviderName = "AwsS3SseCloudStorageProvider",
            Parameters = [],
        };

        var defaultKeyDerivationOptions = KeyDerivationServiceDefaultOptions.Blazor_Argon2id();
        foreach (var curParam in defaultKeyDerivationOptions.Parameters)
        {
            manifest.Parameters[$"KeyDerivationService_{curParam.Key}"] = curParam.Value;
        }

        this.mockCloudStorageProvider.Setup(x => x.GetObjectAsync(
            It.Is<string>(y => y == $"{this.prefix}/{id}/manifest.json"),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                var json = System.Text.Json.JsonSerializer.Serialize(manifest);
                var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
                stream.Seek(0, SeekOrigin.Begin);
                return stream;
            });

        this.mockAwsEncryptedCloudStorageProviderTransformer.Setup(
            x => x.CreateSseProvider())
            .Returns(new AwsS3SseCCloudStorageProvider(
                "foo",
                Mock.Of<IAmazonS3Client>()));

        // Act & Assert
        await Assert.ThrowsAnyAsync<CompressionServiceNotSupportedByVaultManagerBootstrapperException>(async () =>
        {
            await this.sut.CreateVaultManagerForVaultAsync(id, cancellationTokenSource.Token);
        });
    }

    [Fact]
    public async Task GivenId_AndManifestContainsGZipCompression_AndManifestContainsUnsupportedCryptoService_WhenCreateVaultManagerForVaultAsync_ThenExceptionThrown()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        var id = Guid.NewGuid().ToString();
        var manifest = new VaultManifest
        {
            ClypseCoreVersion = "1.0.0",
            CompressionServiceName = "GZipCompressionService",
            CryptoServiceName = "Foobar",
            EncryptedCloudStorageProviderName = "AwsS3SseCloudStorageProvider",
            Parameters = [],
        };

        var defaultKeyDerivationOptions = KeyDerivationServiceDefaultOptions.Blazor_Argon2id();
        foreach (var curParam in defaultKeyDerivationOptions.Parameters)
        {
            manifest.Parameters[$"KeyDerivationService_{curParam.Key}"] = curParam.Value;
        }

        this.mockCloudStorageProvider.Setup(x => x.GetObjectAsync(
            It.Is<string>(y => y == $"{this.prefix}/{id}/manifest.json"),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                var json = System.Text.Json.JsonSerializer.Serialize(manifest);
                var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
                stream.Seek(0, SeekOrigin.Begin);
                return stream;
            });

        this.mockAwsEncryptedCloudStorageProviderTransformer.Setup(
            x => x.CreateSseProvider())
            .Returns(new AwsS3SseCCloudStorageProvider(
                "foo",
                Mock.Of<IAmazonS3Client>()));

        // Act & Assert
        await Assert.ThrowsAnyAsync<CryptoServiceNotSupportedByVaultManagerBootstrapperException>(async () =>
        {
            await this.sut.CreateVaultManagerForVaultAsync(id, cancellationTokenSource.Token);
        });
    }

    [Fact]
    public async Task GivenId_AndManifestContainsGZipCompression_AndManifestContainsBouncyCastleAesGcmCrypto_AndManifestContainsUnsupportedEncryptedCloudStorageProvider_WhenCreateVaultManagerForVaultAsync_ThenExceptionThrown()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        var id = Guid.NewGuid().ToString();
        var manifest = new VaultManifest
        {
            ClypseCoreVersion = "1.0.0",
            CompressionServiceName = "GZipCompressionService",
            CryptoServiceName = "NativeAesGcmCryptoService",
            EncryptedCloudStorageProviderName = "Foobar",
            Parameters = [],
        };

        var defaultKeyDerivationOptions = KeyDerivationServiceDefaultOptions.Blazor_Argon2id();
        foreach (var curParam in defaultKeyDerivationOptions.Parameters)
        {
            manifest.Parameters[$"KeyDerivationService_{curParam.Key}"] = curParam.Value;
        }

        this.mockCloudStorageProvider.Setup(x => x.GetObjectAsync(
            It.Is<string>(y => y == $"{this.prefix}/{id}/manifest.json"),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                var json = System.Text.Json.JsonSerializer.Serialize(manifest);
                var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
                stream.Seek(0, SeekOrigin.Begin);
                return stream;
            });

        this.mockAwsEncryptedCloudStorageProviderTransformer.Setup(
            x => x.CreateSseProvider())
            .Returns(new AwsS3SseCCloudStorageProvider(
                "foo",
                Mock.Of<IAmazonS3Client>()));

        // Act & Assert
        await Assert.ThrowsAnyAsync<EncryptedCloudStorageProviderNotSupportedByVaultManagerBootstrapperException>(async () =>
        {
            await this.sut.CreateVaultManagerForVaultAsync(id, cancellationTokenSource.Token);
        });
    }

    [Fact]
    public async Task GivenCancellationToken_WhenListVaultIdsAsync_ThenVaultsListed()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();

        var vaults = new List<string>
        {
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
        };

        this.mockCloudStorageProvider.Setup(x => x.ListObjectsAsync(
            It.Is<string>(y => y == $"{this.prefix}/"),
            It.Is<string>(y => y == "/"),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(vaults);

        // Act
        var result = await this.sut.ListVaultIdsAsync(cancellationTokenSource.Token);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(vaults.Count, result.Count);
        Assert.All(vaults, v => Assert.Contains(v, result));
    }
}
