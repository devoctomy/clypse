using Blazing.Mvvm.ComponentModel;
using clypse.core.Enums;
using clypse.core.Secrets.Import;
using clypse.portal.Models.Import;
using CommunityToolkit.Mvvm.Input;
using Microsoft.AspNetCore.Components.Forms;

namespace clypse.portal.Application.ViewModels;

/// <summary>
/// ViewModel for the import secrets dialog.
/// </summary>
public partial class ImportSecretsDialogViewModel : ViewModelBase
{
    private readonly ISecretsImporterService secretsImporterService;

    private readonly List<CsvImportDataFormat> availableFormats =
    [
        CsvImportDataFormat.KeePassCsv1_x,
        CsvImportDataFormat.Cachy1_x,
    ];

    private string? selectedFileName;
    private string? csvContent;
    private CsvImportDataFormat selectedFormat = CsvImportDataFormat.KeePassCsv1_x;
    private bool isImporting;
    private string? errorMessage;
    private List<string>? headers;
    private List<Dictionary<string, string>>? previewData;

    /// <summary>
    /// Initializes a new instance of <see cref="ImportSecretsDialogViewModel"/>.
    /// </summary>
    /// <param name="secretsImporterService">The secrets importer service.</param>
    public ImportSecretsDialogViewModel(ISecretsImporterService secretsImporterService)
    {
        this.secretsImporterService = secretsImporterService ?? throw new ArgumentNullException(nameof(secretsImporterService));
    }

    /// <summary>Gets the name of the selected file.</summary>
    public string? SelectedFileName { get => selectedFileName; private set => SetProperty(ref selectedFileName, value); }

    /// <summary>Gets or sets the selected CSV import format.</summary>
    public CsvImportDataFormat SelectedFormat { get => selectedFormat; set => SetProperty(ref selectedFormat, value); }

    /// <summary>Gets a value indicating whether an import is in progress.</summary>
    public bool IsImporting { get => isImporting; private set => SetProperty(ref isImporting, value); }

    /// <summary>Gets the current error message.</summary>
    public string? ErrorMessage { get => errorMessage; private set => SetProperty(ref errorMessage, value); }

    /// <summary>Gets the column headers from the loaded CSV.</summary>
    public List<string>? Headers { get => headers; private set => SetProperty(ref headers, value); }

    /// <summary>Gets the preview rows from the loaded CSV.</summary>
    public List<Dictionary<string, string>>? PreviewData { get => previewData; private set => SetProperty(ref previewData, value); }

    /// <summary>Gets the list of available import formats.</summary>
    public IReadOnlyList<CsvImportDataFormat> AvailableFormats => availableFormats;

    /// <summary>Gets a value indicating whether import can proceed.</summary>
    public bool CanImport => !string.IsNullOrEmpty(csvContent) && !IsImporting;

    /// <summary>Gets or sets the callback invoked when the user cancels.</summary>
    public Func<Task>? OnCancelCallback { get; set; }

    /// <summary>Gets or sets the callback invoked when the import completes.</summary>
    public Func<ImportResult, Task>? OnImportCallback { get; set; }

    /// <summary>
    /// Returns the display name for a CSV import format.
    /// </summary>
    /// <param name="format">The CSV import data format to get the display name for.</param>
    public static string GetFormatDisplayName(CsvImportDataFormat format)
    {
        return format switch
        {
            CsvImportDataFormat.KeePassCsv1_x => "KeePass CSV (v1.x)",
            CsvImportDataFormat.Cachy1_x => "Cachy CSV (v1.x)",
            _ => format.ToString()
        };
    }

    /// <summary>
    /// Resets the dialog state (called when dialog is hidden).
    /// </summary>
    public void Reset()
    {
        SelectedFileName = null;
        csvContent = null;
        SelectedFormat = CsvImportDataFormat.KeePassCsv1_x;
        IsImporting = false;
        ErrorMessage = null;
        Headers = null;
        PreviewData = null;
        OnPropertyChanged(nameof(CanImport));
    }

    /// <summary>
    /// Handles a file being selected by the user.
    /// </summary>
    /// <param name="e">The file change event arguments containing the selected file.</param>
    public async Task HandleFileSelectedAsync(InputFileChangeEventArgs e)
    {
        ErrorMessage = null;
        Headers = null;
        PreviewData = null;

        try
        {
            var file = e.File;
            if (file != null)
            {
                SelectedFileName = file.Name;

                if (!file.Name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    ErrorMessage = "Please select a CSV file.";
                    SelectedFileName = null;
                    return;
                }

                if (file.Size > 10 * 1024 * 1024)
                {
                    ErrorMessage = "File size cannot exceed 10MB.";
                    SelectedFileName = null;
                    return;
                }

                using var reader = new StreamReader(file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024));
                csvContent = await reader.ReadToEndAsync();

                PreviewCsvData();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error reading file: {ex.Message}";
            SelectedFileName = null;
            csvContent = null;
        }

        OnPropertyChanged(nameof(CanImport));
    }

    /// <summary>
    /// Executes the import operation.
    /// </summary>
    [RelayCommand]
    public async Task ImportAsync()
    {
        if (string.IsNullOrEmpty(csvContent))
        {
            return;
        }

        try
        {
            IsImporting = true;
            ErrorMessage = null;
            OnPropertyChanged(nameof(CanImport));

            if (secretsImporterService.ImportedSecrets.Count == 0)
            {
                ErrorMessage = "No valid data found in the CSV file.";
                return;
            }

            var mappedSecrets = secretsImporterService.MapImportedSecrets(SelectedFormat);

            var result = new ImportResult
            {
                Success = true,
                ImportedCount = mappedSecrets.Count,
                MappedSecrets = mappedSecrets,
                Format = SelectedFormat,
            };

            if (OnImportCallback != null)
            {
                await OnImportCallback(result);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error importing secrets: {ex.Message}";
        }
        finally
        {
            IsImporting = false;
            OnPropertyChanged(nameof(CanImport));
        }
    }

    /// <summary>
    /// Cancels the import dialog.
    /// </summary>
    [RelayCommand]
    public async Task CancelAsync()
    {
        if (OnCancelCallback != null)
        {
            await OnCancelCallback();
        }
    }

    private void PreviewCsvData()
    {
        if (string.IsNullOrEmpty(csvContent))
        {
            return;
        }

        try
        {
            var importCount = secretsImporterService.ReadData(csvContent);
            if (importCount > 0)
            {
                Headers = secretsImporterService.ImportedHeaders.ToList();
                PreviewData = secretsImporterService.ImportedSecrets.ToList();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error parsing CSV: {ex.Message}";
            Headers = null;
            PreviewData = null;
        }
    }
}
