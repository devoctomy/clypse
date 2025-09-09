namespace clypse.core.Data;

/// <summary>
/// Interface for services that load dictionaries.
/// </summary>
public interface IDictionaryLoaderService
{
    /// <summary>
    /// Loads a gzip compressed dictionary by name.
    /// </summary>
    /// <param name="dictionaryName">The name of the dictionary to load.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="HashSet{String}"/> containing the words in the dictionary.</returns>
    public Task<HashSet<string>> LoadCompressedDictionaryAsync(
        string dictionaryName,
        CancellationToken cancellationToken);

    /// <summary>
    /// Loads a dictionary by name.
    /// </summary>
    /// <param name="dictionaryName">The name of the dictionary to load.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="HashSet{String}"/> containing the words in the dictionary.</returns>
    public Task<HashSet<string>> LoadDictionaryAsync(
        string dictionaryName,
        CancellationToken cancellationToken);
}
