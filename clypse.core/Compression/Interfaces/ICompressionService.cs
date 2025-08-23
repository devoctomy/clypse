namespace clypse.core.Compression.Interfaces;

public interface ICompressionService
{
    /// <summary>
    /// Compresses data from the input stream and writes to the output stream
    /// </summary>
    /// <param name="inputStream">Stream containing data to compress.</param>
    /// <param name="outputStream">Stream to write compressed data to.</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task CompressAsync(
        Stream inputStream,
        Stream outputStream);

    /// <summary>
    /// Decompresses data from the input stream and writes to the output stream
    /// </summary>
    /// <param name="inputStream">Stream containing data to decompress.</param>
    /// <param name="outputStream">Stream to write decompressed data to</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task DecompressAsync(
        Stream inputStream,
        Stream outputStream);
}