using clypse.core.Enums;

namespace clypse.core.Secrets.Import;

/// <summary>
/// Provides field mappings for different CSV import data formats.
/// </summary>
public class FieldMappings
{
    /// <summary>
    /// Gets dictionary of field mappings for KeePass CSV format version 1.x.
    /// </summary>
    public static Dictionary<string, string> KeePassCsv1_x => new ()
        {
            { "Account", "Name" },
            { "Username", "UserName" },
            { "Password", "Password" },
            { "Web site", "WebsiteUrl" },
            { "Comments", "Comments" },
        };

    /// <summary>
    /// Gets dictionary of field mappings for Cachy CSV format version 1.x.
    /// </summary>
    public static Dictionary<string, string> CachyCsv1_x => new ()
        {
            { "Name", "Name" },
            { "Description", "Description" },
            { "Username", "UserName" },
            { "Password", "Password" },
            { "Website", "WebsiteUrl" },
            { "Notes", "Comments" },
        };

    /// <summary>
    /// Gets the field mappings for the specified CSV import data format.
    /// </summary>
    /// <param name="csvImportDataFormat">The CSV import data format.</param>
    /// <returns>A dictionary containing the field mappings.</returns>
    public static Dictionary<string, string> GetMappingsForCsvImportDataFormat(CsvImportDataFormat csvImportDataFormat)
    {
        switch (csvImportDataFormat)
        {
            case CsvImportDataFormat.KeePassCsv1_x:
                return KeePassCsv1_x;
            case CsvImportDataFormat.Cachy1_x:
                return CachyCsv1_x;

            default:
                throw new ArgumentOutOfRangeException(nameof(csvImportDataFormat), csvImportDataFormat, null);
        }
    }
}
