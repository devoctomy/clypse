using System.Collections.Concurrent;
using System.IO.Compression;
using System.Reflection;

namespace Clypse.Core.Data;

/// <summary>
/// Service for loading embedded resources. Each list is loaded once and then kept, so register it as a singleton.
/// </summary>
public class EmbeddedResourceLoaderService : IEmbeddedResourceLoaderService
{
    private readonly ConcurrentDictionary<string, HashSet<string>> cachedHashSets = [];

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
        if (this.cachedHashSets.TryGetValue(resourceKey, out var cachedHashSet))
        {
            return cachedHashSet;
        }

        using var compressedStream = OpenResource(resourceKey, resourceAssembly);
        using var decompressedStream = new GZipStream(compressedStream, CompressionMode.Decompress);
        var lines = await ReadLinesAsync(decompressedStream, cancellationToken);
        return this.cachedHashSets.GetOrAdd(resourceKey, lines);
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
        if (this.cachedHashSets.TryGetValue(resourceKey, out var cachedHashSet))
        {
            return cachedHashSet;
        }

        using var stream = OpenResource(resourceKey, resourceAssembly);
        var lines = await ReadLinesAsync(stream, cancellationToken);
        return this.cachedHashSets.GetOrAdd(resourceKey, lines);
    }

    private static Stream OpenResource(string resourceKey, Assembly? resourceAssembly)
    {
        var assembly = resourceAssembly ?? Assembly.GetExecutingAssembly();
        return assembly.GetManifestResourceStream(resourceKey)
            ?? throw new InvalidOperationException($"Resource '{resourceKey}' not found.");
    }

    private static async Task<HashSet<string>> ReadLinesAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(stream);
        var lines = new HashSet<string>();
        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
        {
            lines.Add(line);
        }

        return lines;
    }
}
