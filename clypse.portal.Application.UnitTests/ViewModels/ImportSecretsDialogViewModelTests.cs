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

    [Fact]
    public void Constructor_SetsDefaults()
    {
        var sut = this.CreateSut();
        Assert.False(sut.IsImporting);
        Assert.Null(sut.SelectedFileName);
        Assert.Null(sut.ErrorMessage);
        Assert.Equal(CsvImportDataFormat.KeePassCsv1_x, sut.SelectedFormat);
        Assert.False(sut.CanImport);
    }

    [Fact]
    public void Reset_ClearsAllState()
    {
        var sut = this.CreateSut();
        sut.SelectedFormat = CsvImportDataFormat.Cachy1_x;

        sut.Reset();

        Assert.Null(sut.SelectedFileName);
        Assert.Equal(CsvImportDataFormat.KeePassCsv1_x, sut.SelectedFormat);
        Assert.Null(sut.ErrorMessage);
        Assert.False(sut.CanImport);
    }

    [Fact]
    public async Task ImportAsync_WhenNoContent_DoesNotCallCallback()
    {
        var sut = this.CreateSut();
        var called = false;
        sut.OnImportCallback = _ => { called = true; return Task.CompletedTask; };

        await sut.ImportCommand.ExecuteAsync(null);

        Assert.False(called);
    }

    [Fact]
    public async Task CancelAsync_InvokesCallback()
    {
        var sut = this.CreateSut();
        var called = false;
        sut.OnCancelCallback = () => { called = true; return Task.CompletedTask; };

        await sut.CancelCommand.ExecuteAsync(null);

        Assert.True(called);
    }

    [Theory]
    [InlineData(CsvImportDataFormat.KeePassCsv1_x, "KeePass CSV (v1.x)")]
    [InlineData(CsvImportDataFormat.Cachy1_x, "Cachy CSV (v1.x)")]
    public void GetFormatDisplayName_ReturnsCorrectName(CsvImportDataFormat format, string expected)
    {
        Assert.Equal(expected, ImportSecretsDialogViewModel.GetFormatDisplayName(format));
    }
}
