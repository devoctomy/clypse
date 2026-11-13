using System.Collections.Concurrent;
using System.IO.Compression;
using System.Reflection;

namespace Clypse.Core.Data;

/// <summary>
/// Service for loading embedded resources. Each list is loaded once and then kept, so register it as a singleton.
/// </summary>
public class EmbeddedResourceLoaderService : IEmbeddedResourceLoaderService
{
    /// <summary>
    /// How much of a compressed list is read at a time. Between reads the load yields, so a large list does not hold up
    /// the page (WebAssembly runs the app on the browser's one thread).
    /// </summary>
    public const int ReadChunkSize = 1024 * 1024;

    private const int InitialHashCapacity = 1024;

    private readonly ConcurrentDictionary<string, HashSet<string>> cachedHashSets = [];
    private readonly ConcurrentDictionary<string, Lazy<Task<HashedTextSet>>> hashedTextSets = [];

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">Thrown if the specified resource is not found.</exception>
    public Task<HashedTextSet> LoadCompressedHashedTextSetAsync(
        string resourceKey,
        Assembly? resourceAssembly,
        CancellationToken cancellationToken)
    {
        // The resource is opened here, so a missing one throws at once and is not kept as a failed load.
        var assembly = resourceAssembly ?? Assembly.GetExecutingAssembly();
        if (assembly.GetManifestResourceInfo(resourceKey) == null)
        {
            throw new InvalidOperationException($"Resource '{resourceKey}' not found.");
        }

        var load = this.hashedTextSets.GetOrAdd(
            resourceKey,
            key => new Lazy<Task<HashedTextSet>>(() => ReadHashedLinesAsync(key, assembly))).Value;

        // The load is started even for a caller that has already given up, so the next caller finds it running. The
        // token is checked here because WaitAsync returns a load that has already finished as it is, cancelled or not.
        return cancellationToken.IsCancellationRequested
            ? Task.FromCanceled<HashedTextSet>(cancellationToken)
            : load.WaitAsync(cancellationToken);
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

    /// <summary>
    /// Reads a gzip compressed list and hashes each line's bytes as they are, with no strings made, so the list's
    /// text is never all in memory at once.
    /// </summary>
    private static async Task<HashedTextSet> ReadHashedLinesAsync(string resourceKey, Assembly assembly)
    {
        // The first caller gets its task back at once, and waits on it like any other.
        await Task.Yield();

        using var compressedStream = OpenResource(resourceKey, assembly);
        using var stream = new GZipStream(compressedStream, CompressionMode.Decompress);
        var hashes = new List<ulong>(InitialHashCapacity);
        var buffer = new byte[ReadChunkSize];
        var carried = 0;
        var isFirstLine = true;
        int read;
        while ((read = await stream.ReadAsync(buffer.AsMemory(carried, buffer.Length - carried))) > 0)
        {
            var filled = carried + read;
            var start = 0;
            int end;
            while ((end = Array.IndexOf(buffer, (byte)'\n', start, filled - start)) >= 0)
            {
                AddLine(hashes, buffer.AsSpan(start, end - start), ref isFirstLine);
                start = end + 1;
            }

            // Keep the unfinished last line for the next read. A line as long as the whole buffer is cut there.
            carried = filled - start;
            if (carried == buffer.Length)
            {
                AddLine(hashes, buffer, ref isFirstLine);
                carried = 0;
            }
            else if (carried > 0)
            {
                Buffer.BlockCopy(buffer, start, buffer, 0, carried);
            }

            await Task.Yield();
        }

        if (carried > 0)
        {
            AddLine(hashes, buffer.AsSpan(0, carried), ref isFirstLine);
        }

        return new HashedTextSet([.. hashes]);
    }

    private static void AddLine(List<ulong> hashes, ReadOnlySpan<byte> line, ref bool isFirstLine)
    {
        if (isFirstLine)
        {
            isFirstLine = false;
            if (line.StartsWith((ReadOnlySpan<byte>)[0xEF, 0xBB, 0xBF]))
            {
                line = line[3..];
            }
        }

        if (line.EndsWith((ReadOnlySpan<byte>)[(byte)'\r']))
        {
            line = line[..^1];
        }

        if (!line.IsEmpty)
        {
            hashes.Add(HashedTextSet.Hash(line));
        }
    }
}
