using System.Security.Cryptography;
using System.Text;

namespace clypse.core.Cryptogtaphy;

/// <summary>
/// Implementation of IRandomGeneratorService.
/// </summary>
public class RandomGeneratorService : IRandomGeneratorService, IDisposable
{
    private readonly RandomNumberGenerator randomNumberGenerator;
    private bool disposed = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="RandomGeneratorService"/> class.
    /// </summary>
    public RandomGeneratorService()
    {
        this.randomNumberGenerator = RandomNumberGenerator.Create();
    }

    /// <summary>
    /// Generates a cryptographically secure array of random bytes.
    /// </summary>
    /// <param name="length">The number of random bytes to generate.</param>
    /// <returns>An array of cryptographically secure random bytes.</returns>
    public byte[] GetRandomBytes(int length)
    {
        this.ThrowIfDisposed();
        var data = new byte[length];
        this.randomNumberGenerator.GetBytes(data, 0, length);
        return data;
    }

    /// <summary>
    /// Generates a cryptographically secure random double between 0.0 and 1.0.
    /// </summary>
    /// <returns>A cryptographically secure random double.</returns>
    public double GetRandomDouble()
    {
        this.ThrowIfDisposed();
        var bytes = new byte[8];
        this.randomNumberGenerator.GetBytes(bytes);
        var unscaled = BitConverter.ToUInt64(bytes, 0);
        unscaled &= (1UL << 53) - 1;
        var random = (double)unscaled / (double)(1UL << 53);
        return random;
    }

    /// <summary>
    /// Generates a cryptographically secure random integer within the specified range [min, max).
    /// </summary>
    /// <param name="min">The inclusive lower bound of the random number returned.</param>
    /// <param name="max">The exclusive upper bound of the random number returned. Must be greater than min.</param>
    /// <returns>A cryptographically secure random integer within the specified range.</returns>
    public int GetRandomInt(
        int min,
        int max)
    {
        this.ThrowIfDisposed();
        var fraction = this.GetRandomDouble();
        var range = max - min;
        var retVal = min + (int)(fraction * range);
        return retVal;
    }

    /// <summary>
    /// Selects a random entry from the provided array.
    /// </summary>
    /// <typeparam name="T">The type of elements in the array.</typeparam>
    /// <param name="array">The array from which to select a random entry.</param>
    /// <returns>A random entry from the array.</returns>
    public T GetRandomArrayEntry<T>(Array array)
    {
        this.ThrowIfDisposed();
        using var rng = RandomNumberGenerator.Create();
        return (T)array.GetValue(this.GetRandomInt(0, array.Length)) !;
    }

    /// <summary>
    /// Generates a random string of the specified length using the provided set of valid characters.
    /// </summary>
    /// <param name="length">The length of the random string to generate.</param>
    /// <param name="validCharacters">A string containing the set of valid characters to use for generating the random string.</param>
    /// <returns>A random string of the specified length composed of characters from the validCharacters set.</returns>
    public string GetRandomStringContainingCharacters(
        int length,
        string validCharacters)
    {
        this.ThrowIfDisposed();
        var sb = new StringBuilder(length);
        for (var i = 0; i < length; i++)
        {
            sb.Append(this.GetRandomArrayEntry<char>(validCharacters.ToCharArray()));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Randomises the order of elements in the provided list.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <param name="list">The list to randomise.</param>
    /// <returns>A new list with the elements in random order.</returns>
    public List<T> RandomiseList<T>(List<T> list)
    {
        var shuffled = new List<T>();
        while (list.Count > 0)
        {
            if (list.Count == 1)
            {
                shuffled.Add(list[0]);
                break;
            }

            var index = this.GetRandomInt(0, list.Count);
            shuffled.Add(list[index]);
            list.RemoveAt(index);
        }

        return shuffled;
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the RandomGeneratorService and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!this.disposed)
        {
            if (disposing)
            {
                this.randomNumberGenerator?.Dispose();
            }

            this.disposed = true;
        }
    }

    /// <summary>
    /// Throws an ObjectDisposedException if the service has been disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(this.disposed, nameof(RandomGeneratorService));
    }
}
