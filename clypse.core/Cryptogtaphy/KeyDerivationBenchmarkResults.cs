namespace clypse.core.Cryptogtaphy;

/// <summary>
/// Results container for key derivation benchmarks.
/// </summary>
/// <param name="results">Results to instantiate the instance with.</param>
public class KeyDerivationBenchmarkResults(List<KeyDerivationBenchmarkResult> results)
{
    /// <summary>
    /// Gets a list of key derivation benchmark results.
    /// </summary>
    public IReadOnlyList<KeyDerivationBenchmarkResult> Results => results;
}
