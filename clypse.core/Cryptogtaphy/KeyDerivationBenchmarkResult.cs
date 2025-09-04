using clypse.core.Enums;

namespace clypse.core.Cryptogtaphy;

/// <summary>
/// Key derivation results for a specific key derivation algorithm.
/// </summary>
public class KeyDerivationBenchmarkResult
{
    /// <summary>
    /// Gets or sets the algorithm used to generate these benchmark results.
    /// </summary>
    public KeyDerivationAlgorithm Algorithm { get; set; }

    /// <summary>
    /// Gets or sets the list of timings for each operation.
    /// </summary>
    public List<TimeSpan> Timings { get; set; } = new List<TimeSpan>();

    /// <summary>
    /// Gets the average amount of time it took to perform each operation.
    /// </summary>
    public TimeSpan? Average => this.Timings != null ? TimeSpan.FromSeconds(this.Timings.Select(x => x.TotalSeconds).Average()) : null;
}
