using System.Security.Cryptography;
using clypse.core.Cryptogtaphy.Interfaces;

namespace clypse.core.Cryptogtaphy;

/// <summary>
/// Implementation of ICryptoService using AES-GCM encryption
/// Stream format: [nonce][ciphertext][authentication tag].
/// </summary>
public class NativeAesGcmCryptoService : ICryptoService
{
    private const int NonceSize = 12;
    private const int TagSize = 16; // AES-GCM uses a 128-bit authentication tag

    /// <summary>
    /// Encrypt the input stream via AES GCM using the key provided.
    /// </summary>
    /// <param name="inputStream">Input stream to encrypt.</param>
    /// <param name="outputStream">Output stream to store cipher data.</param>
    /// <param name="base64Key">Base64 encoded AES key.</param>
    /// <returns>Nothing.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public async Task EncryptAsync(
        Stream inputStream,
        Stream outputStream,
        string? base64Key)
    {
        ArgumentNullException.ThrowIfNull(inputStream, nameof(inputStream));
        ArgumentNullException.ThrowIfNull(outputStream, nameof(outputStream));
        ArgumentException.ThrowIfNullOrEmpty(base64Key, nameof(base64Key));

        byte[] key = Convert.FromBase64String(base64Key);

        byte[] nonce = new byte[NonceSize];
        RandomNumberGenerator.Fill(nonce);

        await outputStream.WriteAsync(nonce);

        using var memoryStream = new MemoryStream();
        await inputStream.CopyToAsync(memoryStream);
        byte[] plaintext = memoryStream.ToArray();

        byte[] ciphertext = new byte[plaintext.Length];
        byte[] tag = new byte[TagSize];
        using (var aesGcm = new AesGcm(key, TagSize))
        {
            aesGcm.Encrypt(nonce, plaintext, ciphertext, tag);
        }

        await outputStream.WriteAsync(ciphertext);
        await outputStream.WriteAsync(tag);
    }

    /// <summary>
    /// Decrypt the input stream via AES GCM using the key provided.
    /// </summary>
    /// <param name="inputStream">Input stream to decrypt.</param>
    /// <param name="outputStream">Output stream to store plaintext data.</param>
    /// <param name="base64Key">Base64 encoded AES key.</param>
    /// <returns>Nothing.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task DecryptAsync(
        Stream inputStream,
        Stream outputStream,
        string? base64Key)
    {
        ArgumentNullException.ThrowIfNull(inputStream, nameof(inputStream));
        ArgumentNullException.ThrowIfNull(outputStream, nameof(outputStream));
        ArgumentNullException.ThrowIfNull(base64Key, nameof(base64Key));

        if (string.IsNullOrEmpty(base64Key))
        {
            throw new ArgumentException("Key cannot be empty", nameof(base64Key));
        }

        byte[] key = Convert.FromBase64String(base64Key);
        byte[] nonce = new byte[NonceSize];
        int bytesRead = await inputStream.ReadAsync(nonce.AsMemory(0, NonceSize));
        if (bytesRead != NonceSize)
        {
            throw new InvalidOperationException($"Failed to read nonce from input stream. Expected {NonceSize} bytes but got {bytesRead}.");
        }

        using var memoryStream = new MemoryStream();
        await inputStream.CopyToAsync(memoryStream);
        byte[] encryptedData = memoryStream.ToArray();
        if (encryptedData.Length < TagSize)
        {
            throw new InvalidOperationException("Input data is too short to contain authentication tag.");
        }

        int ciphertextLength = encryptedData.Length - TagSize;
        byte[] ciphertext = new byte[ciphertextLength];
        byte[] tag = new byte[TagSize];

        Buffer.BlockCopy(encryptedData, 0, ciphertext, 0, ciphertextLength);
        Buffer.BlockCopy(encryptedData, ciphertextLength, tag, 0, TagSize);

        byte[] plaintext = new byte[ciphertextLength];
        using (var aesGcm = new AesGcm(key, TagSize))
        {
            aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);
        }

        await outputStream.WriteAsync(plaintext);
    }
}