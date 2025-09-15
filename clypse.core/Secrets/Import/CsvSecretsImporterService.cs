using clypse.core.Secrets.Import.Exceptions;
using Microsoft.VisualBasic.FileIO;

namespace clypse.core.Secrets.Import;

/// <summary>
/// Service for importing secrets from CSV format.
/// </summary>
public class CsvSecretsImporterService : ISecretsImporterService
{
    private readonly List<string> importedHeaders = [];
    private readonly List<Dictionary<string, string>> importedSecrets = [];

    /// <summary>
    /// Gets the list of headers imported from the data.
    /// </summary>
    public IReadOnlyList<string> ImportedHeaders => this.importedHeaders;

    /// <summary>
    /// Gets the list of secrets imported from the data.
    /// </summary>
    public IReadOnlyList<Dictionary<string, string>> ImportedSecrets => this.importedSecrets;

    /// <summary>
    /// Reads all data to import into memory.
    /// </summary>
    /// <param name="data">The data string containing secrets to import.</param>
    /// <returns>The number of secrets successfully read into memory.</returns>
    public int ReadData(string data)
    {
        using var reader = new StringReader(data);
        using var parser = new TextFieldParser(reader);
        parser.TextFieldType = FieldType.Delimited;
        parser.HasFieldsEnclosedInQuotes = true;
        parser.TrimWhiteSpace = true;
        parser.SetDelimiters(",");

        string[]? headers = parser.ReadFields();
        if (headers == null ||
            headers.Length == 0)
        {
            throw new CsvDataMissingHeadersException();
        }

        this.importedHeaders.AddRange(headers);
        while (!parser.EndOfData)
        {
            string[]? values = parser.ReadFields();
            if (values == null ||
                values.Length == 0 ||
                values.Length != headers.Length)
            {
                continue;
            }

            var row = new Dictionary<string, string>();
            for (var i = 0; i < values.Length; i++)
            {
                var header = headers[i];
                row.Add(header, values[i]);
            }

            this.importedSecrets.Add(row);
        }

        return this.importedSecrets.Count;
    }
}
