namespace clypse.core.Cryptogtaphy;

/// <summary>
/// Defines methods for generating cryptographically secure random values.
/// </summary>
public interface IRandomGeneratorService
{
    /// <summary>
    /// Generates a cryptographically secure array of random bytes.
    /// </summary>
    /// <param name="length">The number of random bytes to generate.</param>
    /// <returns>An array of cryptographically secure random bytes.</returns>
    public byte[] GetRandomBytes(int length);

    /// <summary>
    /// Generates a cryptographically secure random double between 0.0 and 1.0.
    /// </summary>
    /// <returns>A cryptographically secure random double.</returns>
    public double GetRandomDouble();

    /// <summary>
    /// Generates a cryptographically secure random integer within the specified range [min, max).
    /// </summary>
    /// <param name="min">The inclusive lower bound of the random number returned.</param>
    /// <param name="max">The exclusive upper bound of the random number returned. Must be greater than min.</param>
    /// <returns>A cryptographically secure random integer within the specified range.</returns>
    public int GetRandomInt(
        int min,
        int max);

    /// <summary>
    /// Selects a random entry from the provided array.
    /// </summary>
    /// <typeparam name="T">The type of elements in the array.</typeparam>
    /// <param name="array">The array from which to select a random entry.</param>
    /// <returns>A random entry from the array.</returns>
    public T GetRandomArrayEntry<T>(Array array);

    /// <summary>
    /// Generates a random string of the specified length using the provided set of valid characters.
    /// </summary>
    /// <param name="length">The length of the random string to generate.</param>
    /// <param name="validCharacters">A string containing the set of valid characters to use for generating the random string.</param>
    /// <returns>A random string of the specified length composed of characters from the validCharacters set.</returns>
    public string GetRandomStringContainingCharacters(
        int length,
        string validCharacters);
}
