using System.Security;
using clypse.core.Cryptography;
using clypse.core.Enums;

namespace clypse.core.Cryptogtaphy;

/// <summary>
/// Implementation of KeyDerivationService.
/// </summary>
public class KeyDerivationService : IKeyDerivationService
{
    /// <summary>
    /// Derive a key from a password, using a specified key derivation algorithm.
    /// </summary>
    /// <param name="keyDerivationAlgorithm">Key derivation algorithm to use.</param>
    /// <param name="passphrase">Passphrase as a SecureString.</param>
    /// <param name="base64Salt">The base64-encoded salt for key derivation.</param>
    /// <returns>A byte array containing the derived cryptographic key.</returns>
    public async Task<byte[]> DeriveKeyFromPassphraseAsync(
        KeyDerivationAlgorithm keyDerivationAlgorithm,
        SecureString passphrase,
        string base64Salt)
    {
        return keyDerivationAlgorithm switch
        {
            KeyDerivationAlgorithm.Rfc2898 => await CryptoHelpers.DeriveKeyFromPassphraseUsingRfc2898Async(passphrase, base64Salt),
            KeyDerivationAlgorithm.Argon2 => await CryptoHelpers.DeriveKeyFromPassphraseUsingArgon2Async(passphrase, base64Salt),
            _ => throw new NotImplementedException($"KeyDerivationAlgorithm '{keyDerivationAlgorithm}' not supported by KeyDerivationService."),
        };
    }
}
