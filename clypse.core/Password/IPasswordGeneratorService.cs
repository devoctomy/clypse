using clypse.core.Enums;

namespace clypse.core.Password;

/// <summary>
/// Interface for password generation service.
/// </summary>
public interface IPasswordGeneratorService
{
    /// <summary>
    /// Loads a dictionary of words based on the specified dictionary type.
    /// </summary>
    /// <param name="dictionaryType">The type of dictionary to load.</param>
    /// <returns>A list of words from the specified dictionary.</returns>
    public List<string> LoadDictionary(DictionaryType dictionaryType);

    /// <summary>
    /// Generates a memorable password based on the provided template.
    /// </summary>
    /// <param name="template">Template to use for password generation.</param>
    /// <returns>Returns a password adhering to the format specified by the provided template.</returns>
    public string GenerateMemorablePassword(string template);
}
