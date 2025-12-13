using System.Security;
using clypse.core.Enums;

namespace clypse.core.Cryptography.Interfaces;

/// <summary>
/// Interface key derivation serivce, which key derivation from a single place.
/// </summary>
public interface IKeyDerivationService : IDisposable
{
    /// <summary>
    /// Gets options to use for key derivation.
    /// </summary>
    public KeyDerivationServiceOptions Options { get; }

    /// <summary>
    /// Derive a key from a password, using a specified key derivation algorithm.
    /// </summary>
    /// <param name="passphrase">The passphrase to derive the key from.</param>
    /// <param name="base64Salt">The base64-encoded salt for key derivation.</param>
    /// <returns>A byte array containing the derived cryptographic key.</returns>
    public Task<byte[]> DeriveKeyFromPassphraseAsync(
        string passphrase,
        string base64Salt);
}
