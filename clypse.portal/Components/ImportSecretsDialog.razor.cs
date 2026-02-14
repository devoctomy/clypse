using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using clypse.core.Enums;
using clypse.core.Secrets.Import;
using clypse.portal.Models.Import;

namespace clypse.portal.Components;

public partial class ImportSecretsDialog : ComponentBase
{
    [Parameter] public bool Show { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }
    [Parameter] public EventCallback<ImportResult> OnImport { get; set; }
    [Inject] public ISecretsImporterService SecretsImporterService { get; set; } = default!;

    private string? selectedFileName;
    private string? csvContent;
    private CsvImportDataFormat selectedFormat = CsvImportDataFormat.KeePassCsv1_x;
    private bool IsImporting;
    private string? ErrorMessage;
    private List<string>? headers;
    private List<Dictionary<string, string>>? previewData;

    private readonly List<CsvImportDataFormat> availableFormats = new()
    {
        CsvImportDataFormat.KeePassCsv1_x,
        CsvImportDataFormat.Cachy1_x
    };

    private bool CanImport => !string.IsNullOrEmpty(csvContent) && !IsImporting;

    protected override void OnParametersSet()
    {
        if (!Show)
        {
            // Reset dialog state when hidden
            selectedFileName = null;
            csvContent = null;
            selectedFormat = CsvImportDataFormat.KeePassCsv1_x;
            IsImporting = false;
            ErrorMessage = null;
            headers = null;
            previewData = null;
        }
    }

    private async Task HandleFileSelected(InputFileChangeEventArgs e)
    {
        ErrorMessage = null;
        headers = null;
        previewData = null;

        try
        {
            var file = e.File;
            if (file != null)
            {
                selectedFileName = file.Name;

                // Validate file type
                if (!file.Name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    ErrorMessage = "Please select a CSV file.";
                    selectedFileName = null;
                    return;
                }

                // Validate file size (limit to 10MB)
                if (file.Size > 10 * 1024 * 1024)
                {
                    ErrorMessage = "File size cannot exceed 10MB.";
                    selectedFileName = null;
                    return;
                }

                // Read file content
                using var reader = new StreamReader(file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024));
                csvContent = await reader.ReadToEndAsync();

                // Try to preview the data
                await PreviewData();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error reading file: {ex.Message}";
            selectedFileName = null;
            csvContent = null;
        }

        StateHasChanged();
    }

    private Task PreviewData()
    {
        if (string.IsNullOrEmpty(csvContent))
            return Task.CompletedTask;

        try
        {
            // Use the importer service to read and preview the data
            var importCount = SecretsImporterService.ReadData(csvContent);
            
            if (importCount > 0)
            {
                headers = SecretsImporterService.ImportedHeaders.ToList();
                previewData = SecretsImporterService.ImportedSecrets.ToList();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error parsing CSV: {ex.Message}";
            headers = null;
            previewData = null;
        }

        return Task.CompletedTask;
    }

    private async Task OnImportClick()
    {
        if (string.IsNullOrEmpty(csvContent))
            return;

        try
        {
            IsImporting = true;
            ErrorMessage = null;
            StateHasChanged();

            if (SecretsImporterService.ImportedSecrets.Count == 0)
            {
                ErrorMessage = "No valid data found in the CSV file.";
                return;
            }

            // Map the imported secrets to the selected format
            var mappedSecrets = SecretsImporterService.MapImportedSecrets(selectedFormat);

            var result = new ImportResult
            {
                Success = true,
                ImportedCount = mappedSecrets.Count,
                MappedSecrets = mappedSecrets,
                Format = selectedFormat
            };

            await OnImport.InvokeAsync(result);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error importing secrets: {ex.Message}";
        }
        finally
        {
            IsImporting = false;
            StateHasChanged();
        }
    }

    private string GetFormatDisplayName(CsvImportDataFormat format)
    {
        return format switch
        {
            CsvImportDataFormat.KeePassCsv1_x => "KeePass CSV (v1.x)",
            CsvImportDataFormat.Cachy1_x => "Cachy CSV (v1.x)",
            _ => format.ToString()
        };
    }

    private async Task OnCancelClick()
    {
        await OnCancel.InvokeAsync();
    }
}