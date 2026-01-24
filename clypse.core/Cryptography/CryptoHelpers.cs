using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;
using Org.BouncyCastle.Asn1.Pkcs;

namespace clypse.core.Cryptography;

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
    [Obsolete("Use RandomNumberGenerator instead.")]
    public static byte[] GenerateRandomBytes(int length)
    {
        using var rng = RandomNumberGenerator.Create();
        var data = new byte[length];
        rng.GetBytes(data, 0, length);
        return data;
    }

    /// <summary>
    /// Generates a cryptographically secure random double between 0.0 and 1.0.
    /// </summary>
    /// <returns>A cryptographically secure random double.</returns>
    [Obsolete("Use RandomNumberGenerator instead.")]
    public static double GetRandomDouble()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[8];
        rng.GetBytes(bytes);
        var unscaled = BitConverter.ToUInt64(bytes, 0);
        unscaled &= (1UL << 53) - 1;
        var random = (double)unscaled / (double)(1UL << 53);
        return random;
    }

    /// <summary>
    /// Generates a cryptographically secure random integer within the specified range [min, max).
    /// </summary>
    /// <param name="min">The inclusive lower bound of the random number returned.</param>
    /// <param name="max">The exclusive upper bound of the random number returned. Must be greater than min.</param>
    /// <returns>A cryptographically secure random integer within the specified range.</returns>
    [Obsolete("Use RandomNumberGenerator instead.")]
    public static int GetRandomInt(
        int min,
        int max)
    {
        var fraction = GetRandomDouble();
        var range = max - min;
        var retVal = min + (int)(fraction * range);
        return retVal;
    }

    /// <summary>
    /// Selects a random entry from the provided array.
    /// </summary>
    /// <typeparam name="T">The type of elements in the array.</typeparam>
    /// <param name="array">The array from which to select a random entry.</param>
    /// <returns>A random entry from the array.</returns>
    [Obsolete("Use RandomNumberGenerator instead.")]
    public static T GetRandomArrayEntry<T>(Array array)
    {
        using var rng = RandomNumberGenerator.Create();
        return (T)array.GetValue(GetRandomInt(0, array.Length)) !;
    }

    /// <summary>
    /// Generates a random string of the specified length using the provided set of valid characters.
    /// </summary>
    /// <param name="length">The length of the random string to generate.</param>
    /// <param name="validCharacters">A string containing the set of valid characters to use for generating the random string.</param>
    /// <returns>A random string of the specified length composed of characters from the validCharacters set.</returns>
    [Obsolete("Use RandomNumberGenerator instead.")]
    public static string GetRandomStringContainingCharacters(
        int length,
        string validCharacters)
    {
        var sb = new StringBuilder(length);
        for (var i = 0; i < length; i++)
        {
            sb.Append(GetRandomArrayEntry<char>(validCharacters.ToCharArray()));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Computes a SHA-256 hash of the specified string and returns a truncated portion of the hash. Uses a default length of 16 bytes.
    /// </summary>
    /// <param name="value">The string value to hash.</param>
    /// <returns>A byte array containing the truncated SHA-256 hash.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when length is greater than the hash size.</exception>
    public static byte[] Sha256HashString(string value)
    {
        return Sha256HashString(value, 16);
    }

    /// <summary>
    /// Computes a SHA-256 hash of the specified string and returns a truncated portion of the hash.
    /// </summary>
    /// <param name="value">The string value to hash.</param>
    /// <param name="length">The number of bytes to return from the hash.</param>
    /// <returns>A byte array containing the truncated SHA-256 hash.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when length is greater than the hash size.</exception>
    public static byte[] Sha256HashString(
        string value,
        int length)
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
    /// <param name="passphrase">The passphrase to derive the key from.</param>
    /// <param name="base64Salt">The base64-encoded salt for key derivation.</param>
    /// <param name="keyLength">The desired length of the derived key in bytes (default: 32).</param>
    /// <param name="degreeOfParallelism">Number of threads to use when deriving the key. This is wasted in WASM.</param>
    /// <param name="memorySize">How much RAM Argon2id forces the computation to use.</param>
    /// <param name="iterations">How many times Argon2id runs over that allocated memory.</param>
    /// <returns>A byte array containing the derived cryptographic key.</returns>
    public static async Task<byte[]> DeriveKeyFromPassphraseUsingArgon2idAsync(
        string passphrase,
        string base64Salt,
        int keyLength = 32,
        int degreeOfParallelism = 16,
        int memorySize = 8192,
        int iterations = 40)
    {
        var argon2 = new Argon2id(Encoding.UTF8.GetBytes(passphrase))
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
    /// <param name="passphrase">The passphrase to derive the key from.</param>
    /// <param name="base64Salt">The base64-encoded salt for key derivation.</param>
    /// <param name="keyLength">The desired length of the derived key in bytes (default: 32).</param>
    /// <param name="iterations">The number of iterations to perform (default: 100000).</param>
    /// <returns>A byte array containing the derived cryptographic key.</returns>
    public static async Task<byte[]> DeriveKeyFromPassphraseUsingRfc2898Async(
        string passphrase,
        string base64Salt,
        int keyLength = 32,
        int iterations = 100000)
    {
        return await Task.Run(() =>
        {
            var passphraseBytes = Encoding.UTF8.GetBytes(passphrase);
            var salt = Convert.FromBase64String(base64Salt);

            var bytes = Rfc2898DeriveBytes.Pbkdf2(passphraseBytes, salt, iterations, HashAlgorithmName.SHA256, keyLength);
            return bytes;
        });
    }
}
