using System.Buffers;
using System.IO.Hashing;
using System.Text;

namespace Clypse.Core.Data;

/// <summary>
/// A large set of text kept as a sorted array of 64-bit hashes of each item's UTF-8 bytes, not as strings. The weak
/// known passwords list has over five million entries: as strings it takes several hundred megabytes, too much for a
/// browser tab, and as hashes about 40.
/// </summary>
/// <remarks>
/// A text not in the set is reported as in it only if its hash equals one of the items', which for a few million items
/// is about one chance in a few trillion.
/// </remarks>
public sealed class HashedTextSet
{
    private const int StackBufferSize = 256;

    private readonly ulong[] hashes;

    /// <summary>
    /// Initializes a new instance of the <see cref="HashedTextSet"/> class.
    /// </summary>
    /// <param name="hashes">The items' hashes (see <see cref="Hash(ReadOnlySpan{byte})"/>). The array is sorted and kept,
    /// not copied.</param>
    public HashedTextSet(ulong[] hashes)
    {
        ArgumentNullException.ThrowIfNull(hashes);

        Array.Sort(hashes);
        this.hashes = hashes;
    }

    /// <summary>
    /// Gets how many hashes the set holds.
    /// </summary>
    public int Count => this.hashes.Length;

    /// <summary>
    /// Hashes an item's UTF-8 bytes, as the set keeps it.
    /// </summary>
    /// <param name="utf8">The item, as UTF-8.</param>
    /// <returns>The hash.</returns>
    public static ulong Hash(ReadOnlySpan<byte> utf8) => XxHash3.HashToUInt64(utf8);

    /// <summary>
    /// Checks whether a text is in the set, exactly (case and all).
    /// </summary>
    /// <param name="text">The text.</param>
    /// <returns>True if it is in the set.</returns>
    public bool Contains(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var length = Encoding.UTF8.GetMaxByteCount(text.Length);
        byte[]? rented = null;
        var buffer = length <= StackBufferSize
            ? stackalloc byte[StackBufferSize]
            : (rented = ArrayPool<byte>.Shared.Rent(length));
        try
        {
            var written = Encoding.UTF8.GetBytes(text, buffer);
            return Array.BinarySearch(this.hashes, Hash(buffer[..written])) >= 0;
        }
        finally
        {
            if (rented != null)
            {
                ArrayPool<byte>.Shared.Return(rented, clearArray: true);
            }

            buffer.Clear();
        }
    }
}
