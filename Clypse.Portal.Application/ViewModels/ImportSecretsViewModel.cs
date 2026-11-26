using Blazing.Mvvm.ComponentModel;
using Clypse.Core.Base;
using Clypse.Core.Base.Exceptions;
using Clypse.Core.Enums;
using Clypse.Core.Extensions;
using Clypse.Core.Secrets;
using Clypse.Core.Secrets.Import;
using Clypse.Portal.Application.Exceptions;
using Clypse.Portal.Application.Services.Interfaces;
using Clypse.Portal.Application.Text;
using Devoctomy.Blazwiz.Core.Text.Interfaces;
using Devoctomy.Blazwiz.UI.Text.Interfaces;
using Microsoft.Extensions.Localization;

namespace Clypse.Portal.Application.ViewModels;

/// <summary>
/// ViewModel for importing secrets from a CSV file into the open vault. The file is read in the chosen format, its rows
/// become logins, and they are added only if every one is valid.
/// </summary>
public partial class ImportSecretsViewModel : ViewModelBase
{
    /// <summary>
    /// The largest file read, in bytes.
    /// </summary>
    public const long MaximumFileSize = 10 * 1024 * 1024;

    /// <summary>
    /// The formats offered, in order.
    /// </summary>
    public static readonly IReadOnlyList<CsvImportDataFormat> Formats = [CsvImportDataFormat.KeePassCsv1_x, CsvImportDataFormat.Cachy1_x, CsvImportDataFormat.ClypseJson];

    private readonly IVaultSession session;
    private readonly ISecretsImporterService importer;
    private readonly IJsonSecretsImporterService jsonImporter;
    private readonly IErrorText errorText;
    private readonly ITextPlurals plurals;
    private readonly IStringLocalizer<ClypseText> text;

    private CsvImportDataFormat format = CsvImportDataFormat.KeePassCsv1_x;
    private string? content;
    private string? fileName;
    private IReadOnlyList<Secret> found = [];
    private bool isWorking;
    private int written;
    private int imported;
    private string? error;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImportSecretsViewModel"/> class.
    /// </summary>
    /// <param name="session">The open vault, which the secrets are added to.</param>
    /// <param name="importer">Reads CSV files.</param>
    /// <param name="jsonImporter">Reads clypse's JSON files.</param>
    /// <param name="errorText">Describes errors.</param>
    /// <param name="plurals">Words counts.</param>
    /// <param name="text">Clypse's text.</param>
    public ImportSecretsViewModel(
        IVaultSession session,
        ISecretsImporterService importer,
        IJsonSecretsImporterService jsonImporter,
        IErrorText errorText,
        ITextPlurals plurals,
        IStringLocalizer<ClypseText> text)
    {
        this.session = session ?? throw new ArgumentNullException(nameof(session));
        this.importer = importer ?? throw new ArgumentNullException(nameof(importer));
        this.jsonImporter = jsonImporter ?? throw new ArgumentNullException(nameof(jsonImporter));
        this.errorText = errorText ?? throw new ArgumentNullException(nameof(errorText));
        this.plurals = plurals ?? throw new ArgumentNullException(nameof(plurals));
        this.text = text ?? throw new ArgumentNullException(nameof(text));
    }

    /// <summary>
    /// Gets the file's format.
    /// </summary>
    public CsvImportDataFormat Format
    {
        get => format;
        private set
        {
            if (SetProperty(ref format, value))
            {
                OnPropertyChanged(nameof(FileTypes));
            }
        }
    }

    /// <summary>
    /// Gets the kinds of file the file picker offers for the format.
    /// </summary>
    public string FileTypes => Format == CsvImportDataFormat.ClypseJson ? ".json,application/json" : ".csv,text/csv";

    /// <summary>
    /// Gets the chosen file's name, or null.
    /// </summary>
    public string? FileName
    {
        get => fileName;
        private set => SetProperty(ref fileName, value);
    }

    /// <summary>
    /// Gets the secrets found in the file.
    /// </summary>
    public IReadOnlyList<Secret> Found
    {
        get => found;
        private set
        {
            if (SetProperty(ref found, value))
            {
                OnPropertyChanged(nameof(FoundText));
                OnPropertyChanged(nameof(CanImport));
            }
        }
    }

    /// <summary>
    /// Gets how many secrets were found, as the dialog says it, or null before a file is read.
    /// </summary>
    public string? FoundText => Found.Count == 0 ? null : plurals.Format(text, "Import.Found", Found.Count);

    /// <summary>
    /// Gets a value indicating whether there is something to import.
    /// </summary>
    public bool CanImport => Found.Count > 0 && !IsWorking && imported == 0;

    /// <summary>
    /// Gets a value indicating whether the secrets are being imported.
    /// </summary>
    public bool IsWorking
    {
        get => isWorking;
        private set
        {
            if (SetProperty(ref isWorking, value))
            {
                OnPropertyChanged(nameof(CanImport));
                OnPropertyChanged(nameof(ProgressText));
            }
        }
    }

    /// <summary>
    /// Gets how far the import has got, as the dialog says it, or null when nothing is being imported.
    /// </summary>
    public string? ProgressText => IsWorking ? text["Import.Progress", written, Found.Count].Value : null;

    /// <summary>
    /// Gets how many secrets were imported, as the dialog says it, or null before they are.
    /// </summary>
    public string? DoneText => imported == 0 ? null : plurals.Format(text, "Import.Done", imported);

    /// <summary>
    /// Gets why the file could not be read or imported, or null.
    /// </summary>
    public string? Error
    {
        get => error;
        private set => SetProperty(ref error, value);
    }

    /// <summary>
    /// Gets a format's name, as the dialog shows it.
    /// </summary>
    /// <param name="value">The format.</param>
    /// <returns>The name.</returns>
    public string GetFormatName(CsvImportDataFormat value) => text["Import.Format." + value];

    /// <summary>
    /// Reads the file again in another format.
    /// </summary>
    /// <param name="value">The format.</param>
    public void SetFormat(CsvImportDataFormat value)
    {
        Format = Formats.Contains(value) ? value : Formats[0];
        Read();
    }

    /// <summary>
    /// Reads a file's content in the chosen format.
    /// </summary>
    /// <param name="name">The file's name.</param>
    /// <param name="data">The file's content.</param>
    public void LoadFile(string name, string data)
    {
        FileName = name;
        content = data;
        Read();
    }

    /// <summary>
    /// Imports the secrets found into the open vault.
    /// </summary>
    /// <returns>True if they were imported.</returns>
    public async Task<bool> ImportAsync()
    {
        if (!CanImport)
        {
            return false;
        }

        written = 0;
        IsWorking = true;
        Error = null;
        try
        {
            imported = await session.ImportAsync(Found, new ProgressReporter(this));
            content = null;
            OnPropertyChanged(nameof(DoneText));
            OnPropertyChanged(nameof(CanImport));
            return true;
        }
        catch (Exception ex)
        {
            Error = errorText.Describe(ex);
            return false;
        }
        finally
        {
            IsWorking = false;
        }
    }

    private static Secret ToSecret(Dictionary<string, string> row)
    {
        // The CSV formats hold logins; clypse's own format says what each secret is, and is a login if it does not.
        var secret = Secret.FromDictionary(row);
        if (!row.ContainsKey(nameof(Secret.SecretType)))
        {
            secret.SecretType = SecretType.Web;
        }

        return secret.CastSecretToCorrectType();
    }

    private static IReadOnlyList<string> MissingFields(Secret secret)
    {
        try
        {
            new ClypseObjectValidator(secret).Validate();
            return [];
        }
        catch (ClypseObjectValidatorException ex)
        {
            return ex.MissingProperties;
        }
    }

    private List<Dictionary<string, string>> ReadCsv(string data)
    {
        importer.ReadData(data);
        return importer.MapImportedSecrets(Format);
    }

    private void Read()
    {
        Error = null;
        Found = [];
        if (content == null)
        {
            return;
        }

        try
        {
            var rows = Format == CsvImportDataFormat.ClypseJson ? jsonImporter.Read(content) : ReadCsv(content);
            var secrets = rows
                .Where(row => row.Count > 0)
                .Select(ToSecret)
                .ToList();
            if (secrets.Count == 0)
            {
                Error = errorText.Describe(ClypseErrorCodes.ImportNothingFound, []);
                return;
            }

            // Checked now, not when importing: a file in another format often parses, but its rows have no names.
            var missing = secrets.Select(MissingFields).ToList();
            if (missing.All(m => m.Count > 0))
            {
                Error = errorText.Describe(ClypseErrorCodes.ImportUnreadable, [GetFormatName(Format)]);
                return;
            }

            var firstInvalid = missing.FirstOrDefault(m => m.Count > 0);
            if (firstInvalid != null)
            {
                Error = errorText.Describe(ClypseErrorCodes.SecretInvalid, [string.Join(", ", firstInvalid)]);
                return;
            }

            Found = secrets;
        }
        catch (Exception)
        {
            Error = errorText.Describe(ClypseErrorCodes.ImportUnreadable, [GetFormatName(Format)]);
        }
    }

    /// <summary>
    /// Takes the count of secrets written so far. It reports on the caller's thread, unlike <see cref="Progress{T}"/>,
    /// so the dialog updates as each secret is written.
    /// </summary>
    /// <param name="owner">The view model it reports to.</param>
    private sealed class ProgressReporter(ImportSecretsViewModel owner)
        : IProgress<int>
    {
        public void Report(int value)
        {
            owner.written = value;
            owner.OnPropertyChanged(nameof(ProgressText));
        }
    }
}
