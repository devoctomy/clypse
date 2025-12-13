using System.Security.Cryptography;
using clypse.core.Cryptography.Interfaces;

namespace clypse.core.Cryptography;

/// <summary>
/// Implementation of ICryptoService using AES encryption in CBC mode
/// Stream format: [IV][encrypted data].
/// </summary>
public class NativeAesCbcCryptoService : ICryptoService, IDisposable
{
    private const int IvSize = 16;
    private readonly Aes aes;
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="NativeAesCbcCryptoService"/> class.
    /// </summary>
    public NativeAesCbcCryptoService()
    {
        this.aes = Aes.Create();
        this.aes.Mode = CipherMode.CBC;
        this.aes.Padding = PaddingMode.PKCS7;
    }

    /// <summary>
    /// Encrypt the input stream via AES CBC using the key provided.
    /// </summary>
    /// <param name="inputStream">Input stream to encrypt.</param>
    /// <param name="outputStream">Output stream to store cipher data.</param>
    /// <param name="base64Key">Base64 encoded AES key.</param>
    /// <returns>Nothing.</returns>
    /// <exception cref="ArgumentNullException">Thrown when inputStream, outputStream, or base64Key is null.</exception>
    /// <exception cref="ArgumentException">Thrown when base64Key is empty or not a valid base64 string.</exception>
    public async Task EncryptAsync(
        Stream inputStream,
        Stream outputStream,
        string? base64Key)
    {
        ArgumentNullException.ThrowIfNull(inputStream, nameof(inputStream));
        ArgumentNullException.ThrowIfNull(outputStream, nameof(inputStream));
        ArgumentException.ThrowIfNullOrEmpty(base64Key, nameof(base64Key));

        byte[] key = Convert.FromBase64String(base64Key);
        this.aes.Key = key;
        this.aes.GenerateIV();

        await outputStream.WriteAsync(this.aes.IV.AsMemory(0, this.aes.IV.Length));

        using var encryptor = this.aes.CreateEncryptor();
        using var cryptoStream = new CryptoStream(outputStream, encryptor, CryptoStreamMode.Write, leaveOpen: true);
        await inputStream.CopyToAsync(cryptoStream);
        await cryptoStream.FlushFinalBlockAsync();
    }

    /// <summary>
    /// Decrypt the input stream via AES CBC using the key provided.
    /// </summary>
    /// <param name="inputStream">Input stream to decrypt.</param>
    /// <param name="outputStream">Output stream to store plaintext data.</param>
    /// <param name="base64Key">Base64 encoded AES key.</param>
    /// <returns>Nothing.</returns>
    /// <exception cref="ArgumentNullException">Thrown when inputStream, outputStream, or base64Key is null.</exception>
    /// <exception cref="ArgumentException">Thrown when base64Key is empty or not a valid base64 string.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the input data is invalid or cannot be decrypted.</exception>
    public async Task DecryptAsync(
        Stream inputStream,
        Stream outputStream,
        string? base64Key)
    {
        ArgumentNullException.ThrowIfNull(inputStream, nameof(inputStream));
        ArgumentNullException.ThrowIfNull(outputStream, nameof(inputStream));
        ArgumentException.ThrowIfNullOrEmpty(base64Key, nameof(base64Key));

        byte[] key = Convert.FromBase64String(base64Key);
        this.aes.Key = key;

        byte[] iv = new byte[IvSize];
        int bytesRead = await inputStream.ReadAsync(iv.AsMemory(0, IvSize));

        if (bytesRead != IvSize)
        {
            throw new InvalidOperationException($"Failed to read IV from input stream. Expected {IvSize} bytes but got {bytesRead}.");
        }

        this.aes.IV = iv;

        using var decryptor = this.aes.CreateDecryptor();
        using var cryptoStream = new CryptoStream(inputStream, decryptor, CryptoStreamMode.Read);
        await cryptoStream.CopyToAsync(outputStream);
    }

    /// <summary>
    /// Disposes the resources used by this instance.
    /// </summary>
    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the resources used by this instance.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (this.disposed)
        {
            return;
        }

        if (disposing)
        {
            this.aes?.Dispose();
        }

        this.disposed = true;
    }
}
