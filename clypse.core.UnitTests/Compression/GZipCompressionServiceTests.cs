using System.Text;
using clypse.core.Compression;
using clypse.core.Cryptogtaphy;

namespace clypse.core.UnitTests.Compression;

public class GZipCompressionServiceTests
{
    private readonly GZipCompressionService sut;

    public GZipCompressionServiceTests()
    {
        this.sut = new GZipCompressionService();
    }

    [Fact]
    public async Task GivenPlainTextData_WhenCompressingAndDecompressing_ThenOriginalDataIsReturned()
    {
        // Arrange
        string originalText = "Hello, World! This is a test string for compression.";
        byte[] originalData = Encoding.UTF8.GetBytes(originalText);

        using var inputStream = new MemoryStream(originalData);
        using var compressedStream = new MemoryStream();
        using var decompressedStream = new MemoryStream();

        // Act
        await this.sut.CompressAsync(inputStream, compressedStream, CancellationToken.None);
        compressedStream.Position = 0;
        await this.sut.DecompressAsync(compressedStream, decompressedStream, CancellationToken.None);

        // Assert
        string decompressedText = Encoding.UTF8.GetString(decompressedStream.ToArray());
        Assert.Equal(originalText, decompressedText);
    }

    [Fact]
    public async Task GivenLargeDataStream_WhenCompressingAndDecompressing_ThenDataIsPreservedCorrectly()
    {
        // Arrange
        byte[] largeData = CryptoHelpers.GenerateRandomBytes(1024 * 1024);
        using var inputStream = new MemoryStream(largeData);
        using var compressedStream = new MemoryStream();
        using var decompressedStream = new MemoryStream();

        // Act
        await this.sut.CompressAsync(inputStream, compressedStream, CancellationToken.None);
        compressedStream.Position = 0;
        await this.sut.DecompressAsync(compressedStream, decompressedStream, CancellationToken.None);

        // Assert
        Assert.Equal(largeData, decompressedStream.ToArray());
    }

    [Fact]
    public async Task GivenRepetitiveData_WhenCompressing_ThenDataIsSmallerThanOriginal()
    {
        // Arrange
        string repetitiveText = new ('A', 10000); // 10KB of repeated 'A' characters
        byte[] originalData = Encoding.UTF8.GetBytes(repetitiveText);

        using var inputStream = new MemoryStream(originalData);
        using var compressedStream = new MemoryStream();

        // Act
        await this.sut.CompressAsync(inputStream, compressedStream, CancellationToken.None);

        // Assert
        Assert.True(
            compressedStream.Length < originalData.Length,
            $"Compressed size ({compressedStream.Length}) should be smaller than original size ({originalData.Length})");
    }

    [Fact]
    public async Task GivenEmptyStream_WhenCompressingAndDecompressing_ThenEmptyDataIsReturned()
    {
        // Arrange
        using var inputStream = new MemoryStream();
        using var compressedStream = new MemoryStream();
        using var decompressedStream = new MemoryStream();

        // Act
        await this.sut.CompressAsync(inputStream, compressedStream, CancellationToken.None);
        compressedStream.Position = 0;
        await this.sut.DecompressAsync(compressedStream, decompressedStream, CancellationToken.None);

        // Assert
        Assert.Equal(0, decompressedStream.Length);
    }

    [Fact]
    public async Task GivenInvalidGZipData_WhenDecompressing_ThenThrowsInvalidDataException()
    {
        // Arrange
        byte[] invalidData = Encoding.UTF8.GetBytes("This is not GZip compressed data");
        using var inputStream = new MemoryStream(invalidData);
        using var outputStream = new MemoryStream();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidDataException>(
            async () => await this.sut.DecompressAsync(inputStream, outputStream, CancellationToken.None));
    }

    [Fact]
    public async Task GivenNullInputStream_WhenCompressing_ThenThrowsArgumentNullException()
    {
        // Arrange
        using var outputStream = new MemoryStream();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await this.sut.CompressAsync(null!, outputStream, CancellationToken.None));
    }

    [Fact]
    public async Task GivenNullOutputStream_WhenCompressing_ThenThrowsArgumentNullException()
    {
        // Arrange
        using var inputStream = new MemoryStream();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await this.sut.CompressAsync(inputStream, null!, CancellationToken.None));
    }

    [Fact]
    public async Task GivenNullInputStream_WhenDecompressing_ThenThrowsArgumentNullException()
    {
        // Arrange
        using var outputStream = new MemoryStream();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await this.sut.DecompressAsync(null!, outputStream, CancellationToken.None));
    }

    [Fact]
    public async Task GivenNullOutputStream_WhenDecompressing_ThenThrowsArgumentNullException()
    {
        // Arrange
        using var inputStream = new MemoryStream();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await this.sut.DecompressAsync(inputStream, null!, CancellationToken.None));
    }

    [Fact]
    public async Task GivenBinaryData_WhenCompressingAndDecompressing_ThenDataIsPreservedCorrectly()
    {
        // Arrange
        byte[] binaryData = new byte[1000];
        new Random(123).NextBytes(binaryData);

        using var inputStream = new MemoryStream(binaryData);
        using var compressedStream = new MemoryStream();
        using var decompressedStream = new MemoryStream();

        // Act
        await this.sut.CompressAsync(inputStream, compressedStream, CancellationToken.None);
        compressedStream.Position = 0;
        await this.sut.DecompressAsync(compressedStream, decompressedStream, CancellationToken.None);

        // Assert
        Assert.Equal(binaryData, decompressedStream.ToArray());
    }
}
