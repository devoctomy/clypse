using System.Text;
using clypse.core.Cryptogtaphy;

namespace clypse.core.UnitTests.Cryptography;

/// <summary>
/// Tests to verify compatibility between Native and Bouncy Castle AES-GCM implementations
/// </summary>
public class AesGcmCompatibilityTests : IDisposable
{
    private readonly NativeAesGcmCryptoService _nativeService;
    private readonly BouncyCastleAesGcmCryptoService _bouncyCastleService;
    private readonly string _testKey;

    public AesGcmCompatibilityTests()
    {
        _nativeService = new NativeAesGcmCryptoService();
        _bouncyCastleService = new BouncyCastleAesGcmCryptoService();
        byte[] keyBytes = CryptoHelpers.GenerateRandomBytes(32);
        _testKey = Convert.ToBase64String(keyBytes);
    }

    [Fact]
    public async Task GivenDataEncryptedWithNative_WhenDecryptedWithBouncyCastle_ThenOriginalDataIsReturned()
    {
        // Arrange
        string originalText = "Cross-implementation compatibility test";
        byte[] originalData = Encoding.UTF8.GetBytes(originalText);

        using var inputStream = new MemoryStream(originalData);
        using var encryptedStream = new MemoryStream();
        using var decryptedStream = new MemoryStream();

        // Act - Encrypt with Native, Decrypt with Bouncy Castle
        await _nativeService.EncryptAsync(inputStream, encryptedStream, _testKey);
        encryptedStream.Position = 0;
        await _bouncyCastleService.DecryptAsync(encryptedStream, decryptedStream, _testKey);

        // Assert
        string decryptedText = Encoding.UTF8.GetString(decryptedStream.ToArray());
        Assert.Equal(originalText, decryptedText);
    }

    [Fact]
    public async Task GivenDataEncryptedWithBouncyCastle_WhenDecryptedWithNative_ThenOriginalDataIsReturned()
    {
        // Arrange
        string originalText = "Cross-implementation compatibility test";
        byte[] originalData = Encoding.UTF8.GetBytes(originalText);

        using var inputStream = new MemoryStream(originalData);
        using var encryptedStream = new MemoryStream();
        using var decryptedStream = new MemoryStream();

        // Act - Encrypt with Bouncy Castle, Decrypt with Native
        await _bouncyCastleService.EncryptAsync(inputStream, encryptedStream, _testKey);
        encryptedStream.Position = 0;
        await _nativeService.DecryptAsync(encryptedStream, decryptedStream, _testKey);

        // Assert
        string decryptedText = Encoding.UTF8.GetString(decryptedStream.ToArray());
        Assert.Equal(originalText, decryptedText);
    }

    [Fact]
    public async Task GivenLargeDataEncryptedWithNative_WhenDecryptedWithBouncyCastle_ThenDataIsPreservedCorrectly()
    {
        // Arrange
        byte[] largeData = CryptoHelpers.GenerateRandomBytes(1024 * 1024); // 1MB

        using var inputStream = new MemoryStream(largeData);
        using var encryptedStream = new MemoryStream();
        using var decryptedStream = new MemoryStream();

        // Act - Encrypt with Native, Decrypt with Bouncy Castle
        await _nativeService.EncryptAsync(inputStream, encryptedStream, _testKey);
        encryptedStream.Position = 0;
        await _bouncyCastleService.DecryptAsync(encryptedStream, decryptedStream, _testKey);

        // Assert
        Assert.Equal(largeData, decryptedStream.ToArray());
    }

    [Fact]
    public async Task GivenLargeDataEncryptedWithBouncyCastle_WhenDecryptedWithNative_ThenDataIsPreservedCorrectly()
    {
        // Arrange
        byte[] largeData = CryptoHelpers.GenerateRandomBytes(1024 * 1024); // 1MB

        using var inputStream = new MemoryStream(largeData);
        using var encryptedStream = new MemoryStream();
        using var decryptedStream = new MemoryStream();

        // Act - Encrypt with Bouncy Castle, Decrypt with Native
        await _bouncyCastleService.EncryptAsync(inputStream, encryptedStream, _testKey);
        encryptedStream.Position = 0;
        await _nativeService.DecryptAsync(encryptedStream, decryptedStream, _testKey);

        // Assert
        Assert.Equal(largeData, decryptedStream.ToArray());
    }

    [Fact]
    public async Task GivenEmptyDataEncryptedWithNative_WhenDecryptedWithBouncyCastle_ThenEmptyDataIsReturned()
    {
        // Arrange
        byte[] emptyData = Array.Empty<byte>();

        using var inputStream = new MemoryStream(emptyData);
        using var encryptedStream = new MemoryStream();
        using var decryptedStream = new MemoryStream();

        // Act - Encrypt with Native, Decrypt with Bouncy Castle
        await _nativeService.EncryptAsync(inputStream, encryptedStream, _testKey);
        encryptedStream.Position = 0;
        await _bouncyCastleService.DecryptAsync(encryptedStream, decryptedStream, _testKey);

        // Assert
        Assert.Equal(emptyData, decryptedStream.ToArray());
    }

    [Fact]
    public async Task GivenEmptyDataEncryptedWithBouncyCastle_WhenDecryptedWithNative_ThenEmptyDataIsReturned()
    {
        // Arrange
        byte[] emptyData = Array.Empty<byte>();

        using var inputStream = new MemoryStream(emptyData);
        using var encryptedStream = new MemoryStream();
        using var decryptedStream = new MemoryStream();

        // Act - Encrypt with Bouncy Castle, Decrypt with Native
        await _bouncyCastleService.EncryptAsync(inputStream, encryptedStream, _testKey);
        encryptedStream.Position = 0;
        await _nativeService.DecryptAsync(encryptedStream, decryptedStream, _testKey);

        // Assert
        Assert.Equal(emptyData, decryptedStream.ToArray());
    }

    [Fact]
    public async Task GivenBothImplementations_WhenEncryptingSameData_ThenStreamFormatsAreIdentical()
    {
        // Arrange
        string testData = "Format compatibility test";
        byte[] data = Encoding.UTF8.GetBytes(testData);

        using var inputStream1 = new MemoryStream(data);
        using var inputStream2 = new MemoryStream(data);
        using var nativeEncryptedStream = new MemoryStream();
        using var bouncyCastleEncryptedStream = new MemoryStream();

        // Use the same key and manually set the same nonce for predictable comparison
        byte[] fixedNonce = new byte[12] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
        
        // Act - This test verifies the stream format structure, not exact byte comparison
        // since nonces will be different due to randomization
        await _nativeService.EncryptAsync(inputStream1, nativeEncryptedStream, _testKey);
        await _bouncyCastleService.EncryptAsync(inputStream2, bouncyCastleEncryptedStream, _testKey);

        // Assert - Verify stream structure: nonce (12 bytes) + ciphertext + tag (16 bytes)
        byte[] nativeBytes = nativeEncryptedStream.ToArray();
        byte[] bouncyCastleBytes = bouncyCastleEncryptedStream.ToArray();

        // Both should have the same total length for the same input
        Assert.Equal(nativeBytes.Length, bouncyCastleBytes.Length);
        
        // Both should be: 12 bytes (nonce) + data length + 16 bytes (tag)
        int expectedLength = 12 + data.Length + 16;
        Assert.Equal(expectedLength, nativeBytes.Length);
        Assert.Equal(expectedLength, bouncyCastleBytes.Length);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _nativeService.Dispose();
        _bouncyCastleService.Dispose();
    }
}
