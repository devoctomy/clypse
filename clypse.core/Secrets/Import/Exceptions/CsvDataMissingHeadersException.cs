using clypse.core.Exceptions;

namespace clypse.core.Secrets.Import.Exceptions;

/// <summary>
/// Represents errors that occur when the CSV data is missing headers row.
/// </summary>
public class CsvDataMissingHeadersException : ClypseCoreException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CsvDataMissingHeadersException"/> class.
    /// </summary>
    public CsvDataMissingHeadersException()
    : base("The Csv data is missing headers row which is required for import.")
    {
    }
}
