using clypse.core.Enums;

namespace clypse.portal.Models.Import;

/// <summary>
/// Represents the result of an import operation.
/// </summary>
public class ImportResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the import was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the number of secrets successfully imported.
    /// </summary>
    public int ImportedCount { get; set; }

    /// <summary>
    /// Gets or sets the list of mapped secrets from the import.
    /// </summary>
    public List<Dictionary<string, string>> MappedSecrets { get; set; } = [];

    /// <summary>
    /// Gets or sets the format of the imported data.
    /// </summary>
    public CsvImportDataFormat Format { get; set; }

    /// <summary>
    /// Gets or sets the error message if the import failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}
