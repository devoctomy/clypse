using clypse.core.Extensions;
using Konscious.Security.Cryptography;
using System.Security;
using System.Security.Cryptography;
using System.Text;

namespace clypse.core.Cryptogtaphy;

public class CryptoHelpers
{
    public static byte[] GenerateRandomBytes(int length)
    {
        using var rng = RandomNumberGenerator.Create();
        var data = new byte[length];
        rng.GetBytes(data, 0, length);
        return data;
    }

    public static byte[] SaltFromId(
        string id,
        int saltLen = 16)
    {
        var norm = id.Trim().ToLowerInvariant();
        var h = SHA256.HashData(Encoding.UTF8.GetBytes(norm));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(saltLen, h.Length);

        var salt = new byte[saltLen];
        Buffer.BlockCopy(h, 0, salt, 0, saltLen);
        return salt;
    }

    public static async Task<byte[]> DeriveKeyFromPassphraseAsync(
        SecureString passphrase,
        string base64Salt,
        int keyLength = 32)
    {
        var argon2 = new Argon2d(passphrase.ToUtf8Bytes())
        {
            DegreeOfParallelism = 16,
            MemorySize = 8192,
            Iterations = 40,
            Salt = Convert.FromBase64String(base64Salt)
        };
        return await argon2.GetBytesAsync(keyLength);
    }
}
