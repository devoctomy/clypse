using clypse.core.Cryptogtaphy;
using clypse.core.Enums;

namespace clypse.core.Password;

/// <summary>
/// Interface for password generation service.
/// </summary>
public interface IPasswordGeneratorService
{
    /// <summary>
    /// Gets the random generator service.
    /// </summary>
    public IRandomGeneratorService RandomGeneratorService { get; }

    /// <summary>
    /// Gets a dictionary of words from cache or loads it and caches it.
    /// </summary>
    /// <param name="dictionaryType">The type of dictionary to load.</param>
    /// <returns>A list of words from the specified dictionary.</returns>
    public List<string> GetOrLoadDictionary(DictionaryType dictionaryType);

    /// <summary>
    /// Generates a memorable password based on the provided template.
    /// </summary>
    /// <param name="template">Template to use for password generation.</param>
    /// <returns>Returns a password adhering to the format specified by the provided template.</returns>
    public string GenerateMemorablePassword(string template);

    /// <summary>
    /// Generates a random password based on the specified character groups and length.
    /// </summary>
    /// <param name="groups">Character groups to include in the password.</param>
    /// <param name="length">Length of the password to generate.</param>
    /// <returns>A randomly generated password.</returns>
    public string GenerateRandomPassword(
        CharacterGroup groups,
        int length,
        bool atLeastOneOfEachGroup);
}
