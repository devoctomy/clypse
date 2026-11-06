using Clypse.Core.Exceptions;

namespace Clypse.Core.Secrets.Import.Exceptions;

/// <summary>
/// Represents errors that occur when the CSV import data format is not specified.
/// </summary>
public class CsvImportDataFormatNotSpecifiedException : ClypseCoreException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CsvImportDataFormatNotSpecifiedException"/> class.
    /// </summary>
    public CsvImportDataFormatNotSpecifiedException()
    : base($"The Csv import data format was not specifed.")
    {
    }
}
