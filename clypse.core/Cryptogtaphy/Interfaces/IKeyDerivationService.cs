using System.Security;
using clypse.core.Enums;

namespace clypse.core.Cryptogtaphy.Interfaces;

/// <summary>
/// Interface key derivation serivce, which key derivation from a single place.
/// </summary>
public interface IKeyDerivationService
{
    /// <summary>
    /// Derive a key from a password, using a specified key derivation algorithm.
    /// </summary>
    /// <param name="keyDerivationAlgorithm">Key derivation algorithm to use.</param>
    /// <param name="passphrase">Passphrase as a SecureString.</param>
    /// <param name="base64Salt">The base64-encoded salt for key derivation.</param>
    /// <returns>A byte array containing the derived cryptographic key.</returns>
    public Task<byte[]> DeriveKeyFromPassphraseAsync(
        KeyDerivationAlgorithm keyDerivationAlgorithm,
        SecureString passphrase,
        string base64Salt);

    /// <summary>
    /// Perform a benchmark of all key derivation algorithms currently supported.
    /// </summary>
    /// <param name="count">Number of tests to run for each algorithm.</param>
    /// <returns>Benchmark results.</returns>
    public Task<KeyDerivationBenchmarkResults> BenchmarkAllAsync(int count);
}
