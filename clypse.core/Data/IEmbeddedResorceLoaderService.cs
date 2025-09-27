using System.Reflection;

namespace clypse.core.Data;

/// <summary>
/// Interface for services that load embedded resources.
/// </summary>
public interface IEmbeddedResorceLoaderService
{
    /// <summary>
    /// Loads a gzip compressed hash set from an embedded resource.
    /// </summary>
    /// <param name="resourceKey">The key of the embedded resource to load.</param>
    /// <param name="resourceAssembly">The assembly containing the embedded resource.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="HashSet{String}"/> containing the items in the resource.</returns>
    public Task<HashSet<string>> LoadCompressedHashSetAsync(
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
