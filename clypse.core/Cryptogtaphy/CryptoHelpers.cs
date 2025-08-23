using clypse.core.Extensions;
using Konscious.Security.Cryptography;
using System.Security;
using System.Security.Cryptography;

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
