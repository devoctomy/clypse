using System.Reflection;

namespace Clypse.Core.Data;

/// <summary>
/// Interface for services that load embedded resources.
/// </summary>
public interface IEmbeddedResourceLoaderService
{
    /// <summary>
    /// Loads a gzip compressed list, one item per line, from an embedded resource as a <see cref="HashedTextSet"/>. It
    /// is loaded once, however many callers ask at the same time, and then kept.
    /// </summary>
    /// <param name="resourceKey">The key of the embedded resource to load.</param>
    /// <param name="resourceAssembly">The assembly containing the embedded resource.</param>
    /// <param name="cancellationToken">A token to stop waiting with. The load itself carries on for later callers.</param>
    /// <returns>The set of the items in the resource.</returns>
    public Task<HashedTextSet> LoadCompressedHashedTextSetAsync(
        string resourceKey,
        Assembly? resourceAssembly,
        CancellationToken cancellationToken);

    /// <summary>
    /// Loads a hash set from an embedded resource.
    /// </summary>
    /// <param name="resourceKey">The key of the embedded resource to load.</param>
    /// <param name="resourceAssembly">The assembly containing the embedded resource.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="HashSet{String}"/> containing the items in the resource.</returns>
    public Task<HashSet<string>> LoadHashSetAsync(
        string resourceKey,
        Assembly? resourceAssembly,
        CancellationToken cancellationToken);
}
