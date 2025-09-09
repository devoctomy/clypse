using clypse.core.Cryptogtaphy.Interfaces;
using clypse.core.Enums;

namespace clypse.core.Cryptogtaphy;

/// <summary>
/// Implementation of KeyDerivationService.
/// </summary>
public class KeyDerivationService : IKeyDerivationService, IDisposable
{
    private readonly IRandomGeneratorService randomGeneratorService;
    private readonly KeyDerivationServiceOptions options;
    private bool disposed = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyDerivationService"/> class with the provided options.
    /// </summary>
    /// <param name="randomGeneratorService">An instance of IRandomGeneratorService for generating random values.</param>
    /// <param name="options">Options to use for key derivation.</param>
    public KeyDerivationService(
        IRandomGeneratorService randomGeneratorService,
        KeyDerivationServiceOptions options)
    {
        this.randomGeneratorService = randomGeneratorService;
        this.options = options;
    }

    /// <summary>
    /// Gets options to use for key derivation.
    /// </summary>
    public KeyDerivationServiceOptions Options => this.options;

    /// <summary>
    /// Derive a key from a password, using a specified key derivation algorithm.
    /// </summary>
    /// <param name="passphrase">The passphrase to derive the key from.</param>
    /// <param name="base64Salt">The base64-encoded salt for key derivation.</param>
    /// <returns>A byte array containing the derived cryptographic key.</returns>
    public async Task<byte[]> DeriveKeyFromPassphraseAsync(
        string passphrase,
        string base64Salt)
    {
        this.ThrowIfDisposed();
        var keyDerivationService = this.options.GetAsString(KeyDerivationParameterKeys.Algorithm);
        var algorithm = Enum.Parse<KeyDerivationAlgorithm>(
            keyDerivationService,
            true);
        return algorithm switch
        {
            KeyDerivationAlgorithm.Rfc2898 =>
                await CryptoHelpers.DeriveKeyFromPassphraseUsingRfc2898Async(
                    passphrase,
                    base64Salt,
                    this.options.GetAsInt(KeyDerivationParameterKeys.Rfc2898_KeyLength),
                    this.options.GetAsInt(KeyDerivationParameterKeys.Rfc2898_Iterations)),
            KeyDerivationAlgorithm.Argon2id =>
                await CryptoHelpers.DeriveKeyFromPassphraseUsingArgon2idAsync(
                    passphrase,
                    base64Salt,
                    this.options.GetAsInt(KeyDerivationParameterKeys.Argon2id_KeyLength),
                    this.options.GetAsInt(KeyDerivationParameterKeys.Argon2id_Parallelism),
                    this.options.GetAsInt(KeyDerivationParameterKeys.Argon2id_MemorySizeKb),
                    this.options.GetAsInt(KeyDerivationParameterKeys.Argon2id_Iterations)),
            _ => throw new NotImplementedException($"KeyDerivationAlgorithm '{algorithm}' not supported by KeyDerivationService."),
        };
    }

    /// <summary>
    /// Perform a benchmark of all key derivation algorithms currently supported.
    /// </summary>
    /// <param name="count">Number of tests to run for each algorithm.</param>
    /// <returns>Benchmark results.</returns>
    public async Task<KeyDerivationBenchmarkResults> BenchmarkAllAsync(int count)
    {
        this.ThrowIfDisposed();
        var results = new List<KeyDerivationBenchmarkResult>();
        var algirithmNames = Enum.GetNames<KeyDerivationAlgorithm>();
        var passphrase = "password123";
        var salt = this.randomGeneratorService.GetRandomBytes(16);
        var base64Salt = Convert.ToBase64String(salt);
        foreach (var curAlgorithm in algirithmNames)
        {
            var timings = new List<TimeSpan>();
            var algorithm = Enum.Parse<KeyDerivationAlgorithm>(curAlgorithm, true);
            using var keyDerivationService = new KeyDerivationService(
                new RandomGeneratorService(),
                GetDefaults(algorithm));
            for (var i = 0; i < count; i++)
            {
                var startedAt = DateTime.Now;
                _ = await keyDerivationService.DeriveKeyFromPassphraseAsync(
                    passphrase,
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

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the RandomGeneratorService and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!this.disposed)
        {
            if (disposing)
            {
                ((IDisposable)this.randomGeneratorService)?.Dispose();
            }

            this.disposed = true;
        }
    }

    private static KeyDerivationServiceOptions GetDefaults(KeyDerivationAlgorithm algorithm)
    {
        return algorithm switch
        {
            KeyDerivationAlgorithm.Rfc2898 => KeyDerivationServiceDefaultOptions.Blazor_Rfc2898(),
            KeyDerivationAlgorithm.Argon2id => KeyDerivationServiceDefaultOptions.Blazor_Argon2id(),
            _ => throw new NotImplementedException($"KeyDerivationAlgorithm '{algorithm}' not supported by KeyDerivationService."),
        };
    }

    /// <summary>
    /// Throws an ObjectDisposedException if the service has been disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(this.disposed, nameof(RandomGeneratorService));
    }
}
