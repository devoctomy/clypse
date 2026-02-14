using clypse.core.Enums;

namespace clypse.portal.Models.Import;

public class ImportResult
{
    public bool Success { get; set; }

    public int ImportedCount { get; set; }

    public List<Dictionary<string, string>> MappedSecrets { get; set; } = [];

    public CsvImportDataFormat Format { get; set; }

    public string? ErrorMessage { get; set; }
}
