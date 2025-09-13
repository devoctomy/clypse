using System.Security.Cryptography;
using System.Text;
using clypse.core.Cryptogtaphy;

namespace clypse.core.UnitTests.Cryptography;

public class NativeAesCbcCryptoServiceTests : IDisposable
{
    private readonly RandomGeneratorService randomGeneratorService;
    private readonly NativeAesCbcCryptoService sut;
    private readonly string testKey;

    public NativeAesCbcCryptoServiceTests()
    {
        this.randomGeneratorService = new RandomGeneratorService();
        this.sut = new NativeAesCbcCryptoService();
        byte[] keyBytes = this.randomGeneratorService.GetRandomBytes(32);
        this.testKey = Convert.ToBase64String(keyBytes);
    }

    [Fact]
    public async Task GivenPlainTextData_WhenEncryptingAndDecrypting_ThenOriginalDataIsReturned()
    {
        // Arrange
        string originalText = "Hello, World!";
        byte[] originalData = Encoding.UTF8.GetBytes(originalText);

        using var inputStream = new MemoryStream(originalData);
        using var encryptedStream = new MemoryStream();
        using var decryptedStream = new MemoryStream();

        // Act
        await this.sut.EncryptAsync(inputStream, encryptedStream, this.testKey);
        encryptedStream.Position = 0;
        await this.sut.DecryptAsync(encryptedStream, decryptedStream, this.testKey);

        // Assert
        string decryptedText = Encoding.UTF8.GetString(decryptedStream.ToArray());
        Assert.Equal(originalText, decryptedText);
    }

    [Fact]
    public async Task GivenPlainTextData_AndWrongDecryptionKey_WhenEncryptingAndDecrypting_ThenExceptionThrown()
    {
        // Arrange
        string originalText = "Hello, World!";
        byte[] originalData = Encoding.UTF8.GetBytes(originalText);
        byte[] wrongKeyBytes = new byte[32];

        using var inputStream = new MemoryStream(originalData);
        using var encryptedStream = new MemoryStream();
        using var decryptedStream = new MemoryStream();

        // Assert
        await this.sut.EncryptAsync(inputStream, encryptedStream, this.testKey);
        encryptedStream.Position = 0;
        var exception = await Assert.ThrowsAnyAsync<CryptographicException>(async () =>
        {
            await this.sut.DecryptAsync(encryptedStream, decryptedStream, Convert.ToBase64String(wrongKeyBytes));
        });
        Assert.Equal("Padding is invalid and cannot be removed.", exception.Message);
    }

    [Fact]
    public async Task GivenLargeDataStream_WhenEncryptingAndDecrypting_ThenDataIsPreservedCorrectly()
    {
        // Arrange
        byte[] largeData = this.randomGeneratorService.GetRandomBytes(1024 * 1024);
        using var inputStream = new MemoryStream(largeData);
        using var encryptedStream = new MemoryStream();
        using var decryptedStream = new MemoryStream();

        // Act
        await this.sut.EncryptAsync(inputStream, encryptedStream, this.testKey);
        encryptedStream.Position = 0;
        await this.sut.DecryptAsync(encryptedStream, decryptedStream, this.testKey);

        // Assert
        Assert.Equal(largeData, decryptedStream.ToArray());
    }

    [Theory]
    [InlineData(null)]
    public async Task GivenInvalidKey_WhenEncrypting_ThenThrowsArgumentNullException(string? invalidKey)
    {
        // Arrange
        using var inputStream = new MemoryStream();
        using var outputStream = new MemoryStream();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await this.sut.EncryptAsync(inputStream, outputStream, invalidKey));
    }

    [Theory]
    [InlineData("")]
    public async Task GivenInvalidKey_WhenEncrypting_ThenThrowsArgumentException(string? invalidKey)
    {
        // Arrange
        using var inputStream = new MemoryStream();
        using var outputStream = new MemoryStream();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await this.sut.EncryptAsync(inputStream, outputStream, invalidKey));
    }

    [Theory]
    [InlineData(null)]
    public async Task GivenInvalidKey_WhenDecrypting_ThenThrowsArgumentNullException(string? invalidKey)
    {
        // Arrange
        using var inputStream = new MemoryStream();
        using var outputStream = new MemoryStream();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await this.sut.DecryptAsync(inputStream, outputStream, invalidKey));
    }

    [Theory]
    [InlineData("")]
    public async Task GivenInvalidKey_WhenDecrypting_ThenThrowsArgumentException(string? invalidKey)
    {
        // Arrange
        using var inputStream = new MemoryStream();
        using var outputStream = new MemoryStream();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await this.sut.DecryptAsync(inputStream, outputStream, invalidKey));
    }

    [Fact]
    public async Task GivenShortIVStream_WhenDecrypting_ThenThrowsInvalidOperationException()
    {
        // Arrange
        byte[] shortData = new byte[5]; // Less than IV size (16)
        using var inputStream = new MemoryStream(shortData);
        using var outputStream = new MemoryStream();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await this.sut.DecryptAsync(inputStream, outputStream, this.testKey));

        Assert.Equal("Failed to read IV from input stream. Expected 16 bytes but got 5.", ex.Message);
    }

    [Fact]
    public async Task GivenEmptyStream_WhenDecrypting_ThenThrowsInvalidOperationException()
    {
        // Arrange
        using var inputStream = new MemoryStream();
        using var outputStream = new MemoryStream();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await this.sut.DecryptAsync(inputStream, outputStream, this.testKey));

        Assert.Equal("Failed to read IV from input stream. Expected 16 bytes but got 0.", ex.Message);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        this.sut.Dispose();
    }
}