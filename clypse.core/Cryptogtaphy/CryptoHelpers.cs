using System.Security;
using System.Security.Cryptography;
using System.Text;
using clypse.core.Extensions;
using Konscious.Security.Cryptography;

namespace clypse.core.Cryptogtaphy;

/// <summary>
/// Provides utility methods for cryptographic operations including random byte generation, hashing, and key derivation.
/// </summary>
public class CryptoHelpers
{
    /// <summary>
    /// Generates a cryptographically secure array of random bytes.
    /// </summary>
    /// <param name="length">The number of random bytes to generate.</param>
    /// <returns>An array of cryptographically secure random bytes.</returns>
    public static byte[] GenerateRandomBytes(int length)
    {
        using var rng = RandomNumberGenerator.Create();
        var data = new byte[length];
        rng.GetBytes(data, 0, length);
        return data;
    }

    /// <summary>
    /// Computes a SHA-256 hash of the specified string and returns a truncated portion of the hash.
    /// </summary>
    /// <param name="value">The string value to hash.</param>
    /// <param name="length">The number of bytes to return from the hash (default: 16).</param>
    /// <returns>A byte array containing the truncated SHA-256 hash.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when length is greater than the hash size.</exception>
    public static byte[] Sha256HashString(
        string value,
        int length = 16)
    {
        var norm = value.Trim().ToLowerInvariant();
        var h = SHA256.HashData(Encoding.UTF8.GetBytes(norm));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(length, h.Length);

        var salt = new byte[length];
        Buffer.BlockCopy(h, 0, salt, 0, length);
        return salt;
    }

    /// <summary>
    /// Derives a cryptographic key from a passphrase using the Argon2id key derivation function.
    /// </summary>
    /// <param name="passphrase">The secure passphrase to derive the key from.</param>
    /// <param name="base64Salt">The base64-encoded salt for key derivation.</param>
    /// <param name="keyLength">The desired length of the derived key in bytes (default: 32).</param>
    /// <param name="degreeOfParallelism">Number of threads to use when deriving the key. This is wasted in WASM.</param>
    /// <param name="memorySize">How much RAM Argon2id forces the computation to use.</param>
    /// <param name="iterations">How many times Argon2id runs over that allocated memory.</param>
    /// <returns>A byte array containing the derived cryptographic key.</returns>
    public static async Task<byte[]> DeriveKeyFromPassphraseUsingArgon2idAsync(
        SecureString passphrase,
        string base64Salt,
        int keyLength = 32,
        int degreeOfParallelism = 16,
        int memorySize = 8192,
        int iterations = 40)
    {
        var argon2 = new Argon2id(passphrase.ToUtf8Bytes())
        {
            DegreeOfParallelism = degreeOfParallelism,
            MemorySize = memorySize,
            Iterations = iterations,
            KnownSecret = Sha256HashString("13E3288E-445F-4A44-858C-483B8A3566BC", 32),
            Salt = Convert.FromBase64String(base64Salt),
        };
        return await argon2.GetBytesAsync(keyLength);
    }

    /// <summary>
    /// Derives a cryptographic key from a passphrase using the native .NET PBKDF2 (RFC 2898) key derivation function.
    /// </summary>
    /// <param name="passphrase">The secure passphrase to derive the key from.</param>
    /// <param name="base64Salt">The base64-encoded salt for key derivation.</param>
    /// <param name="keyLength">The desired length of the derived key in bytes (default: 32).</param>
    /// <param name="iterations">The number of iterations to perform (default: 100000).</param>
    /// <returns>A byte array containing the derived cryptographic key.</returns>
    public static async Task<byte[]> DeriveKeyFromPassphraseUsingRfc2898Async(
        SecureString passphrase,
        string base64Salt,
        int keyLength = 32,
        int iterations = 100000)
    {
        return await Task.Run(() =>
        {
            var passphraseBytes = passphrase.ToUtf8Bytes();
            var pop = System.Text.Encoding.UTF8.GetString(passphraseBytes);
            var salt = Convert.FromBase64String(base64Salt);
            using var pbkdf2 = new Rfc2898DeriveBytes(passphraseBytes, salt, iterations, HashAlgorithmName.SHA256);
            return pbkdf2.GetBytes(keyLength);
        });
    }
}
