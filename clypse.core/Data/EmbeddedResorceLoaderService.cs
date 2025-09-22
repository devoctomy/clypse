using System.Reflection;
using clypse.core.Compression;

namespace clypse.core.Data;

/// <summary>
/// Service for loading embedded resources.
/// </summary>
public class EmbeddedResorceLoaderService : IEmbeddedResorceLoaderService
{
    /// <summary>
    /// A cache of loaded hashsets to avoid redundant loading.
    /// </summary>
    public static readonly Dictionary<string, HashSet<string>> CachedHashSets = [];

    /// <summary>
    /// Loads a gzip compressed hash set from an embedded resource.
    /// </summary>
    /// <param name="resourceKey">The key of the embedded resource to load.</param>
    /// <param name="resourceAssembly">The assembly containing the embedded resource.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="HashSet{String}"/> containing the items in the resource.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the specified resource is not found.</exception>
    public async Task<HashSet<string>> LoadCompressedHashSetAsync(
        string resourceKey,
        Assembly? resourceAssembly,
        CancellationToken cancellationToken)
    {
        if (CachedHashSets.TryGetValue(resourceKey, out var cachedHashSet))
        {
            return cachedHashSet;
        }

        var assembly = resourceAssembly ?? Assembly.GetExecutingAssembly();
        using Stream? compressedStream = assembly.GetManifestResourceStream(resourceKey) ?? throw new InvalidOperationException($"Resource '{resourceKey}' not found.");
        var compressionService = new GZipCompressionService();
        var decompressedStream = new MemoryStream();
        await compressionService.DecompressAsync(compressedStream, decompressedStream, cancellationToken);
        decompressedStream.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(decompressedStream);
        string content = await reader.ReadToEndAsync(cancellationToken);
        var lines = content.Split("\r\n");

        CachedHashSets[resourceKey] = new HashSet<string>([.. lines]);

        return CachedHashSets[resourceKey];
    }

    /// <summary>
    /// Loads a hash set from an embedded resource.
    /// </summary>
    /// <param name="resourceKey">The key of the embedded resource to load.</param>
    /// <param name="resourceAssembly">The assembly containing the embedded resource.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="HashSet{String}"/> containing the items in the resource.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the specified resource is not found.</exception>
    public async Task<HashSet<string>> LoadHashSetAsync(
        string resourceKey,
        Assembly? resourceAssembly,
        CancellationToken cancellationToken)
    {
        if (CachedHashSets.TryGetValue(resourceKey, out var cachedHashSet))
        {
            return cachedHashSet;
        }

        var assembly = resourceAssembly ?? Assembly.GetExecutingAssembly();
        using Stream? stream = assembly.GetManifestResourceStream(resourceKey) ?? throw new InvalidOperationException($"Resource '{resourceKey}' not found.");
        using var reader = new StreamReader(stream);
        var lines = new List<string>();
        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
        {
            lines.Add(line);
        }

        CachedHashSets[resourceKey] = new HashSet<string>([.. lines]);

        return CachedHashSets[resourceKey];
    }
}
