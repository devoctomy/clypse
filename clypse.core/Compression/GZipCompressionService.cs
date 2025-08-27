using System.IO.Compression;
using clypse.core.Compression.Interfaces;

namespace clypse.core.Compression;

/// <summary>
/// Implementation of ICompressionService using GZip compression.
/// </summary>
public class GZipCompressionService : ICompressionService
{
    /// <summary>
    /// Compresses data from the input stream using GZip compression and writes to the output stream.
    /// </summary>
    /// <param name="inputStream">Stream containing data to compress.</param>
    /// <param name="outputStream">Stream to write compressed data to.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when inputStream or outputStream is null.</exception>
    public async Task CompressAsync(
        Stream inputStream,
        Stream outputStream,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(inputStream, nameof(inputStream));
        ArgumentNullException.ThrowIfNull(outputStream, nameof(outputStream));

        using var gzipStream = new GZipStream(outputStream, CompressionMode.Compress, leaveOpen: true);
        await inputStream.CopyToAsync(gzipStream, cancellationToken);
        await gzipStream.FlushAsync(cancellationToken);
    }

    /// <summary>
    /// Decompresses data from the input stream using GZip decompression and writes to the output stream.
    /// </summary>
    /// <param name="inputStream">Stream containing compressed data to decompress.</param>
    /// <param name="outputStream">Stream to write decompressed data to.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when inputStream or outputStream is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when input data is not in valid GZip format.</exception>
    public async Task DecompressAsync(
        Stream inputStream,
        Stream outputStream,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(inputStream, nameof(inputStream));
        ArgumentNullException.ThrowIfNull(outputStream, nameof(outputStream));

        using var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress, leaveOpen: true);
        await gzipStream.CopyToAsync(outputStream, cancellationToken);
    }
}
