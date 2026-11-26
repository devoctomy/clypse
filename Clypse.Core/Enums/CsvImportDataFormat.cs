namespace Clypse.Core.Enums;

/// <summary>
/// CSV Import Data Format.
/// </summary>
public enum CsvImportDataFormat
{
    /// <summary>
    /// No specific CSV format.
    /// </summary>
    None = 0,

    /// <summary>
    /// KeePass CSV format version 1.x.
    /// </summary>
    KeePassCsv1_x = 1,

    /// <summary>
    /// Cachy CSV format version 1.x.
    /// </summary>
    Cachy1_x = 2,

    /// <summary>
    /// Clypse's own JSON format, read by <see cref="Secrets.Import.IJsonSecretsImporterService"/> rather than as CSV.
    /// </summary>
    ClypseJson = 3,
}
