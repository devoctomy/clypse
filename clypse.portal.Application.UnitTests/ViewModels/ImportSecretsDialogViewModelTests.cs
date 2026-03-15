using clypse.core.Enums;
using clypse.core.Secrets.Import;
using clypse.portal.Application.ViewModels;
using clypse.portal.Models.Import;
using Moq;

namespace clypse.portal.Application.UnitTests.ViewModels;

public class ImportSecretsDialogViewModelTests
{
    private readonly Mock<ISecretsImporterService> mockImporter;

    public ImportSecretsDialogViewModelTests()
    {
        this.mockImporter = new Mock<ISecretsImporterService>();
        this.mockImporter.Setup(s => s.ImportedHeaders).Returns(new List<string>());
        this.mockImporter.Setup(s => s.ImportedSecrets).Returns(new List<Dictionary<string, string>>());
    }

    private ImportSecretsDialogViewModel CreateSut() =>
        new(this.mockImporter.Object);

    // --- Constructor ---

    [Fact]
    public void GivenNullImporterService_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Arrange / Act / Assert
        Assert.Throws<ArgumentNullException>(() => new ImportSecretsDialogViewModel(null!));
    }

    // --- Initial state ---

    [Fact]
    public void GivenNewInstance_WhenCheckingInitialState_ThenDefaultValuesAreCorrect()
    {
        // Arrange
        var sut = this.CreateSut();

        // Assert
        Assert.False(sut.IsImporting);
        Assert.Null(sut.SelectedFileName);
        Assert.Null(sut.ErrorMessage);
        Assert.Equal(CsvImportDataFormat.KeePassCsv1_x, sut.SelectedFormat);
        Assert.False(sut.CanImport);
    }

    // --- AvailableFormats ---

    [Fact]
    public void GivenNewInstance_WhenGetAvailableFormats_ThenBothFormatsAreReturned()
    {
        // Arrange
        var sut = this.CreateSut();

        // Assert
        Assert.Contains(CsvImportDataFormat.KeePassCsv1_x, sut.AvailableFormats);
        Assert.Contains(CsvImportDataFormat.Cachy1_x, sut.AvailableFormats);
        Assert.Equal(2, sut.AvailableFormats.Count);
    }

    // --- GetFormatDisplayName ---

    [Theory]
    [InlineData(CsvImportDataFormat.KeePassCsv1_x, "KeePass CSV (v1.x)")]
    [InlineData(CsvImportDataFormat.Cachy1_x, "Cachy CSV (v1.x)")]
    public void GivenKnownFormat_WhenGetFormatDisplayName_ThenReturnsCorrectName(CsvImportDataFormat format, string expected)
    {
        // Act
        var result = ImportSecretsDialogViewModel.GetFormatDisplayName(format);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GivenUnknownFormat_WhenGetFormatDisplayName_ThenReturnsToStringFallback()
    {
        // Arrange
        var unknownFormat = (CsvImportDataFormat)999;

        // Act
        var result = ImportSecretsDialogViewModel.GetFormatDisplayName(unknownFormat);

        // Assert
        Assert.Equal("999", result);
    }

    // --- Reset ---

    [Fact]
    public void GivenModifiedState_WhenReset_ThenAllStateIsCleared()
    {
        // Arrange
        var sut = this.CreateSut();
        sut.SelectedFormat = CsvImportDataFormat.Cachy1_x;

        // Act
        sut.Reset();

        // Assert
        Assert.Null(sut.SelectedFileName);
        Assert.Equal(CsvImportDataFormat.KeePassCsv1_x, sut.SelectedFormat);
        Assert.Null(sut.ErrorMessage);
        Assert.False(sut.CanImport);
        Assert.Null(sut.Headers);
        Assert.Null(sut.PreviewData);
    }

    // --- ImportAsync (no CSV content) ---

    [Fact]
    public async Task GivenNoContent_WhenImportAsync_ThenCallbackIsNotInvoked()
    {
        // Arrange
        var sut = this.CreateSut();
        var called = false;
        sut.OnImportCallback = _ => { called = true; return Task.CompletedTask; };

        // Act
        await sut.ImportCommand.ExecuteAsync(null);

        // Assert
        Assert.False(called);
    }

    // --- ImportAsync (with content, no secrets found) ---

    [Fact]
    public async Task GivenContentButNoSecrets_WhenImportAsync_ThenErrorMessageIsSet()
    {
        // Arrange
        var sut = this.CreateSut();
        this.mockImporter.Setup(s => s.ReadData(It.IsAny<string>())).Returns(1);
        this.mockImporter.Setup(s => s.ImportedHeaders).Returns(new List<string> { "col" });
        this.mockImporter.Setup(s => s.ImportedSecrets).Returns(new List<Dictionary<string, string>>());

        await this.SimulateCsvLoadedAsync(sut, "col\nval");

        // Act
        await sut.ImportCommand.ExecuteAsync(null);

        // Assert
        Assert.NotNull(sut.ErrorMessage);
        Assert.False(sut.IsImporting);
    }

    // --- ImportAsync (with content and secrets, with callback) ---

    [Fact]
    public async Task GivenContentWithSecrets_WhenImportAsync_ThenCallbackIsInvoked()
    {
        // Arrange
        var sut = this.CreateSut();
        var mappedSecrets = new List<Dictionary<string, string>> { new() { { "key", "val" } } };
        this.mockImporter.Setup(s => s.ReadData(It.IsAny<string>())).Returns(1);
        this.mockImporter.Setup(s => s.ImportedHeaders).Returns(new List<string> { "col" });
        this.mockImporter.Setup(s => s.ImportedSecrets)
            .Returns(new List<Dictionary<string, string>> { new() { { "col", "val" } } });
        this.mockImporter.Setup(s => s.MapImportedSecrets(It.IsAny<CsvImportDataFormat>())).Returns(mappedSecrets);

        await this.SimulateCsvLoadedAsync(sut, "col\nval");

        ImportResult? capturedResult = null;
        sut.OnImportCallback = r => { capturedResult = r; return Task.CompletedTask; };

        // Act
        await sut.ImportCommand.ExecuteAsync(null);

        // Assert
        Assert.NotNull(capturedResult);
        Assert.True(capturedResult!.Success);
        Assert.Equal(1, capturedResult.ImportedCount);
        Assert.False(sut.IsImporting);
    }

    // --- ImportAsync (callback is null) ---

    [Fact]
    public async Task GivenContentWithSecretsAndNullCallback_WhenImportAsync_ThenNoExceptionIsThrown()
    {
        // Arrange
        var sut = this.CreateSut();
        var mappedSecrets = new List<Dictionary<string, string>> { new() };
        this.mockImporter.Setup(s => s.ReadData(It.IsAny<string>())).Returns(1);
        this.mockImporter.Setup(s => s.ImportedHeaders).Returns(new List<string> { "col" });
        this.mockImporter.Setup(s => s.ImportedSecrets)
            .Returns(new List<Dictionary<string, string>> { new() });
        this.mockImporter.Setup(s => s.MapImportedSecrets(It.IsAny<CsvImportDataFormat>())).Returns(mappedSecrets);
        await this.SimulateCsvLoadedAsync(sut, "col\nval");
        sut.OnImportCallback = null;

        // Act
        await sut.ImportCommand.ExecuteAsync(null);

        // Assert
        Assert.False(sut.IsImporting);
    }

    // --- ImportAsync (exception path) ---

    [Fact]
    public async Task GivenMapImportedSecretsThrows_WhenImportAsync_ThenErrorMessageIsSet()
    {
        // Arrange
        var sut = this.CreateSut();
        this.mockImporter.Setup(s => s.ReadData(It.IsAny<string>())).Returns(1);
        this.mockImporter.Setup(s => s.ImportedHeaders).Returns(new List<string> { "col" });
        this.mockImporter.Setup(s => s.ImportedSecrets)
            .Returns(new List<Dictionary<string, string>> { new() });
        this.mockImporter.Setup(s => s.MapImportedSecrets(It.IsAny<CsvImportDataFormat>()))
            .Throws(new Exception("mapping failed"));
        await this.SimulateCsvLoadedAsync(sut, "col\nval");

        // Act
        await sut.ImportCommand.ExecuteAsync(null);

        // Assert
        Assert.NotNull(sut.ErrorMessage);
        Assert.False(sut.IsImporting);
    }

    // --- PreviewCsvData (via SimulateCsvLoaded helper) ---

    [Fact]
    public async Task GivenCsvContentWithData_WhenPreviewCsvData_ThenHeadersAndPreviewDataAreSet()
    {
        // Arrange
        var sut = this.CreateSut();
        this.mockImporter.Setup(s => s.ReadData(It.IsAny<string>())).Returns(1);
        this.mockImporter.Setup(s => s.ImportedHeaders).Returns(new List<string> { "Name", "Password" });
        this.mockImporter.Setup(s => s.ImportedSecrets)
            .Returns(new List<Dictionary<string, string>> { new() { { "Name", "Test" } } });

        // Act
        await this.SimulateCsvLoadedAsync(sut, "Name,Password\nTest,pass");

        // Assert
        Assert.NotNull(sut.Headers);
        Assert.NotNull(sut.PreviewData);
    }

    [Fact]
    public async Task GivenCsvContentWithNoData_WhenPreviewCsvData_ThenErrorMessageIsSet()
    {
        // Arrange
        var sut = this.CreateSut();
        this.mockImporter.Setup(s => s.ReadData(It.IsAny<string>())).Returns(0);

        // Act
        await this.SimulateCsvLoadedAsync(sut, "empty");

        // Assert
        Assert.NotNull(sut.ErrorMessage);
        Assert.Null(sut.Headers);
        Assert.Null(sut.PreviewData);
    }

    [Fact]
    public async Task GivenReadDataThrows_WhenPreviewCsvData_ThenErrorMessageIsSet()
    {
        // Arrange
        var sut = this.CreateSut();
        this.mockImporter.Setup(s => s.ReadData(It.IsAny<string>())).Throws(new Exception("parse error"));

        // Act
        await this.SimulateCsvLoadedAsync(sut, "bad-data");

        // Assert
        Assert.NotNull(sut.ErrorMessage);
        Assert.Null(sut.Headers);
        Assert.Null(sut.PreviewData);
    }

    // --- CancelAsync ---

    [Fact]
    public async Task GivenCancelCallback_WhenCancelAsync_ThenCallbackIsInvoked()
    {
        // Arrange
        var sut = this.CreateSut();
        var called = false;
        sut.OnCancelCallback = () => { called = true; return Task.CompletedTask; };

        // Act
        await sut.CancelCommand.ExecuteAsync(null);

        // Assert
        Assert.True(called);
    }

    [Fact]
    public async Task GivenNullCancelCallback_WhenCancelAsync_ThenNoExceptionIsThrown()
    {
        // Arrange
        var sut = this.CreateSut();
        sut.OnCancelCallback = null;

        // Act
        await sut.CancelCommand.ExecuteAsync(null);

        // Assert - no exception thrown
        Assert.Null(sut.ErrorMessage);
    }

    private async Task SimulateCsvLoadedAsync(ImportSecretsDialogViewModel sut, string csvText)
    {
        var fakeFile = new FakeBrowserFile("test.csv", csvText);
        var files = new List<Microsoft.AspNetCore.Components.Forms.IBrowserFile> { fakeFile };
        var args = (Microsoft.AspNetCore.Components.Forms.InputFileChangeEventArgs)
            Activator.CreateInstance(
                typeof(Microsoft.AspNetCore.Components.Forms.InputFileChangeEventArgs),
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public,
                null,
                new object[] { (System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Components.Forms.IBrowserFile>)files },
                null)!;
        await sut.HandleFileSelectedAsync(args);
    }

    private sealed class FakeBrowserFile : Microsoft.AspNetCore.Components.Forms.IBrowserFile
    {
        private readonly string content;

        public FakeBrowserFile(string name, string content)
        {
            this.Name = name;
            this.content = content;
        }

        public string Name { get; }
        public DateTimeOffset LastModified => DateTimeOffset.UtcNow;
        public long Size => System.Text.Encoding.UTF8.GetByteCount(this.content);
        public string ContentType => "text/csv";

        public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default)
            => new MemoryStream(System.Text.Encoding.UTF8.GetBytes(this.content));
    }
}
