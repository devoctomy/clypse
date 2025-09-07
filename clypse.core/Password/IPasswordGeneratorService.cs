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
    /// <param name="dictionaryType"></param>
    /// <returns></returns>
    public List<string> LoadDictionary(DictionaryType dictionaryType);

    /// <summary>
    /// Generates a memorable password based on the provided template.
    /// </summary>
    /// <param name="template"></param>
    /// <returns></returns>
    public string GenerateMemorablePassword(string template);
}
