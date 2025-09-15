namespace clypse.core.Secrets.Import;

/// <summary>
/// Service for importing secrets from various formats.
/// </summary>
public interface ISecretsImporterService
{
    /// <summary>
    /// Gets the list of headers imported from the data.
    /// </summary>
    public IReadOnlyList<string> ImportedHeaders { get; }

    /// <summary>
    /// Gets the list of secrets imported from the data.
    /// </summary>
    public IReadOnlyList<Dictionary<string, string>> ImportedSecrets { get; }

    /// <summary>
    /// Reads all data to import into memory.
    /// </summary>
    /// <param name="data">The data string containing secrets to import.</param>
    /// <returns>The number of secrets successfully imported.</returns>
    public int ReadData(string data);
}
