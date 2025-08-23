using System.Security.Cryptography;
using System.Text;
using clypse.core.Cryptogtaphy;

namespace clypse.core.UnitTests.Cryptography;

public class AesGcmCryptoServiceTests : IDisposable
{
    private readonly AesGcmCryptoService _sut;
    private readonly string _testKey;

    public AesGcmCryptoServiceTests()
    {
        _sut = new AesGcmCryptoService();
        byte[] keyBytes = CryptoHelpers.GenerateRandomBytes(32);
        _testKey = Convert.ToBase64String(keyBytes);
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
        await _sut.EncryptAsync(inputStream, encryptedStream, _testKey);
        encryptedStream.Position = 0;
        await _sut.DecryptAsync(encryptedStream, decryptedStream, _testKey);

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
        await _sut.EncryptAsync(inputStream, encryptedStream, _testKey);
        encryptedStream.Position = 0;
        await Assert.ThrowsAnyAsync<CryptographicException>(async () =>
        {
            await _sut.DecryptAsync(encryptedStream, decryptedStream, Convert.ToBase64String(wrongKeyBytes));
        });
    }

    [Fact]
    public async Task GivenLargeDataStream_WhenEncryptingAndDecrypting_ThenDataIsPreservedCorrectly()
    {
        // Arrange
        byte[] largeData = CryptoHelpers.GenerateRandomBytes(1024 * 1024);
        using var inputStream = new MemoryStream(largeData);
        using var encryptedStream = new MemoryStream();
        using var decryptedStream = new MemoryStream();

        // Act
        await _sut.EncryptAsync(inputStream, encryptedStream, _testKey);
        encryptedStream.Position = 0;
        await _sut.DecryptAsync(encryptedStream, decryptedStream, _testKey);

        // Assert
        Assert.Equal(largeData, decryptedStream.ToArray());
    }

    [Fact]
    public async Task GivenTamperedData_WhenDecrypting_ThenThrowsAuthenticationTagMismatchException()
    {
        // Arrange
        string originalText = "Test Data";
        byte[] originalData = Encoding.UTF8.GetBytes(originalText);

        using var inputStream = new MemoryStream(originalData);
        using var encryptedStream = new MemoryStream();
        using var decryptedStream = new MemoryStream();

        // Act - First encrypt the data
        await _sut.EncryptAsync(inputStream, encryptedStream, _testKey);
        
        // Tamper with the encrypted data
        byte[] tamperedData = encryptedStream.ToArray();
        if (tamperedData.Length > 30)
        {
            tamperedData[29] ^= 0x01;
        }

        using var tamperedStream = new MemoryStream(tamperedData);

        // Assert
        await Assert.ThrowsAsync<AuthenticationTagMismatchException>(
            async () => await _sut.DecryptAsync(tamperedStream, decryptedStream, _testKey));
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
            async () => await _sut.EncryptAsync(inputStream, outputStream, invalidKey));
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
            async () => await _sut.EncryptAsync(inputStream, outputStream, invalidKey));
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
            async () => await _sut.DecryptAsync(inputStream, outputStream, invalidKey));
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
            async () => await _sut.DecryptAsync(inputStream, outputStream, invalidKey));
    }

    [Fact]
    public async Task GivenShortNonceStream_WhenDecrypting_ThenThrowsInvalidOperationException()
    {
        // Arrange
        byte[] shortData = new byte[5];
        using var inputStream = new MemoryStream(shortData);
        using var outputStream = new MemoryStream();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _sut.DecryptAsync(inputStream, outputStream, _testKey));
        
        Assert.Equal("Failed to read nonce from input stream. Expected 12 bytes but got 5.", ex.Message);
    }

    [Fact]
    public async Task GivenEmptyStream_WhenDecrypting_ThenThrowsInvalidOperationException()
    {
        // Arrange
        using var inputStream = new MemoryStream();
        using var outputStream = new MemoryStream();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _sut.DecryptAsync(inputStream, outputStream, _testKey));
        
        Assert.Equal("Failed to read nonce from input stream. Expected 12 bytes but got 0.", ex.Message);
    }

    [Fact]
    public async Task GivenDataTooShortForTag_WhenDecrypting_ThenThrowsInvalidOperationException()
    {
        // Arrange
        // Create data that has valid nonce (12 bytes) but not enough data for tag (16 bytes)
        byte[] shortData = new byte[20]; // 12 bytes nonce + 8 bytes data (less than 16 byte tag)
        using var inputStream = new MemoryStream(shortData);
        using var outputStream = new MemoryStream();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _sut.DecryptAsync(inputStream, outputStream, _testKey));
        
        Assert.Equal("Input data is too short to contain authentication tag.", ex.Message);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _sut.Dispose();
    }
}