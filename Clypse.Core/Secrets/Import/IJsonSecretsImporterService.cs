namespace Clypse.Core.Secrets.Import;

/// <summary>
/// Reads secrets from clypse's own JSON format: an array of secrets, each as the data dictionary a secret is stored as
/// (<see cref="Base.ClypseObject.Data"/>). Values may hold any text, line breaks included.
/// </summary>
public interface IJsonSecretsImporterService
{
    /// <summary>
    /// Reads the secrets' data. Only the secrets' own fields are kept: an id, or any key that is not a field, is left
    /// out, so each secret is imported as a new one.
    /// </summary>
    /// <param name="data">The JSON.</param>
    /// <returns>Each secret's data.</returns>
    /// <exception cref="System.Text.Json.JsonException">The JSON is not an array of objects holding text.</exception>
    /// <exception cref="FormatException">A date or time is not in <see cref="Base.ClypseObject.DateFormat"/>.</exception>
    IReadOnlyList<Dictionary<string, string>> Read(string data);
}
