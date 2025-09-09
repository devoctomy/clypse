using System.Reflection;
using clypse.core.Compression;

namespace clypse.core.Data;

/// <summary>
/// Service for loading dictionaries.
/// </summary>
public class DictionaryLoaderService : IDictionaryLoaderService
{
    private readonly Dictionary<string, HashSet<string>> cachedDictionaries = [];

    /// <summary>
    /// Loads a gzip compressed dictionary by name.
    /// </summary>
    /// <param name="dictionaryName">The name of the dictionary to load.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="HashSet{String}"/> containing the words in the dictionary.</returns>
    public async Task<HashSet<string>> LoadCompressedDictionaryAsync(
        string dictionaryName,
        CancellationToken cancellationToken)
    {
        if (this.cachedDictionaries.TryGetValue(dictionaryName, out var cachedDictionary))
        {
            return cachedDictionary;
        }

        var dictionaryKey = $"clypse.core.Data.Dictionaries.weakknownpasswords.txt.gz";
        var assembly = Assembly.GetExecutingAssembly();
        using Stream? compressedStream = assembly.GetManifestResourceStream(dictionaryKey) ?? throw new InvalidOperationException($"Resource '{dictionaryKey}' not found.");
        var compressionService = new GZipCompressionService();
        var decompressedStream = new MemoryStream();
        await compressionService.DecompressAsync(compressedStream, decompressedStream, cancellationToken);
        decompressedStream.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(decompressedStream);
        string content = await reader.ReadToEndAsync(cancellationToken);
        var lines = content.Split("\r\n");

        this.cachedDictionaries[dictionaryName] = new HashSet<string>([.. lines]);

        return this.cachedDictionaries[dictionaryName];
    }

    /// <summary>
    /// Loads a dictionary by name.
    /// </summary>
    /// <param name="dictionaryName">The name of the dictionary to load.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="HashSet{String}"/> containing the words in the dictionary.</returns>
    public async Task<HashSet<string>> LoadDictionaryAsync(
        string dictionaryName,
        CancellationToken cancellationToken)
    {
        if (this.cachedDictionaries.TryGetValue(dictionaryName, out var cachedDictionary))
        {
            return cachedDictionary;
        }

        var dictionaryKey = $"clypse.core.Data.Dictionaries.{dictionaryName}";
        var assembly = Assembly.GetExecutingAssembly();
        using Stream? stream = assembly.GetManifestResourceStream(dictionaryKey) ?? throw new InvalidOperationException($"Resource '{dictionaryKey}' not found.");
        using var reader = new StreamReader(stream);
        var lines = new List<string>();
        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
        {
            lines.Add(line);
        }

        this.cachedDictionaries[dictionaryName] = new HashSet<string>([.. lines]);

        return this.cachedDictionaries[dictionaryName];
    }
}
