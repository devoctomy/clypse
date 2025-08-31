using System.Security;
using clypse.core.Cryptography;
using clypse.core.Enums;
using Microsoft.VisualBasic;

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

    /// <summary>
    /// Perform a benchmark of all key derivation algorithms currently supported.
    /// </summary>
    /// <param name="count">Number of tests to run for each algorithm.</param>
    /// <returns>Benchmark results.</returns>
    public async Task<KeyDerivationBenchmarkResults> BenchmarkAllAsync(int count)
    {
        var results = new List<KeyDerivationBenchmarkResult>();
        var algirithmNames = Enum.GetNames<KeyDerivationAlgorithm>();
        var securePassphrase = new SecureString();
        foreach (var curChar in "password123")
        {
            securePassphrase.AppendChar(curChar);
        }

        var salt = CryptoHelpers.GenerateRandomBytes(16);
        var base64Salt = Convert.ToBase64String(salt);
        foreach (var curAlgorithm in algirithmNames)
        {
            var timings = new List<TimeSpan>();
            var algorithm = Enum.Parse<KeyDerivationAlgorithm>(curAlgorithm, true);
            for (var i = 0; i < count; i++)
            {
                var startedAt = DateTime.Now;
                var key = await this.DeriveKeyFromPassphraseAsync(
                    algorithm,
                    securePassphrase,
                    base64Salt);
                var elapsed = DateTime.Now - startedAt;
                timings.Add(elapsed);
            }

            results.Add(new KeyDerivationBenchmarkResult
            {
                Algorithm = algorithm,
                Timings = timings,
            });
        }

        return new KeyDerivationBenchmarkResults(results);
    }
}
