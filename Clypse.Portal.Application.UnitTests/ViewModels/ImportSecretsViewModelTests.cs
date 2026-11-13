using Clypse.Core.Enums;
using Clypse.Core.Secrets;
using Clypse.Core.Secrets.Import;
using Clypse.Portal.Application.Exceptions;
using Clypse.Portal.Application.Services.Interfaces;
using Clypse.Portal.Application.Text;
using Clypse.Portal.Application.ViewModels;
using Devoctomy.Blazwiz.Testing;
using Moq;

namespace Clypse.Portal.Application.UnitTests.ViewModels;

public class ImportSecretsViewModelTests
{
    private const string KeePassCsv = "\"Account\",\"Username\",\"Password\",\"Web site\",\"Comments\"\r\n\"Gmail\",\"bob\",\"hunter2\",\"https://mail.google.com\",\"\"\r\n\"Bank\",\"bob\",\"correct horse\",\"https://bank.example\",\"savings\"\r\n";

    private readonly FrameworkText text = new(typeof(ClypseText));
    private readonly Mock<IVaultSession> mockSession = new();
    private IReadOnlyList<Secret>? imported;

    public ImportSecretsViewModelTests()
    {
        mockSession.Setup(x => x.ImportAsync(It.IsAny<IReadOnlyList<Secret>>(), It.IsAny<IProgress<int>?>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyList<Secret>, IProgress<int>?, CancellationToken>((s, _, _) => imported = s)
            .ReturnsAsync((IReadOnlyList<Secret> s, IProgress<int>? _, CancellationToken _) => s.Count);
    }

    [Fact]
    public void GivenNullDependencies_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Arrange
        var importer = new CsvSecretsImporterService();
        var t = text.For<ClypseText>();

        // Act & Assert
        Assert.Equal("session", Assert.Throws<ArgumentNullException>(() => new ImportSecretsViewModel(null!, importer, text.Errors, text.Plurals, t)).ParamName);
        Assert.Equal("importer", Assert.Throws<ArgumentNullException>(() => new ImportSecretsViewModel(mockSession.Object, null!, text.Errors, text.Plurals, t)).ParamName);
        Assert.Equal("errorText", Assert.Throws<ArgumentNullException>(() => new ImportSecretsViewModel(mockSession.Object, importer, null!, text.Plurals, t)).ParamName);
        Assert.Equal("plurals", Assert.Throws<ArgumentNullException>(() => new ImportSecretsViewModel(mockSession.Object, importer, text.Errors, null!, t)).ParamName);
        Assert.Equal("text", Assert.Throws<ArgumentNullException>(() => new ImportSecretsViewModel(mockSession.Object, importer, text.Errors, text.Plurals, null!)).ParamName);
    }

    [Fact]
    public void GivenNoFile_WhenShown_ThenNothingCanBeImported()
    {
        // Act
        var sut = CreateSut();

        // Assert
        Assert.Equal(CsvImportDataFormat.KeePassCsv1_x, sut.Format);
        Assert.False(sut.CanImport);
        Assert.Null(sut.FoundText);
        Assert.Null(sut.DoneText);
        Assert.Equal("KeePass CSV (1.x)", sut.GetFormatName(CsvImportDataFormat.KeePassCsv1_x));
        Assert.Equal("Cachy CSV (1.x)", sut.GetFormatName(CsvImportDataFormat.Cachy1_x));
    }

    [Fact]
    public void GivenAKeePassFile_WhenLoaded_ThenItsRowsAreLogins()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.LoadFile("export.csv", KeePassCsv);

        // Assert
        Assert.Equal("export.csv", sut.FileName);
        Assert.Null(sut.Error);
        Assert.True(sut.CanImport);
        Assert.Equal("2 secrets found.", sut.FoundText);
        var gmail = Assert.IsType<WebSecret>(sut.Found[0]);
        Assert.Equal("Gmail", gmail.Name);
        Assert.Equal("bob", gmail.UserName);
        Assert.Equal("hunter2", gmail.Password);
        Assert.Equal("https://mail.google.com", gmail.WebsiteUrl);
        Assert.Equal(SecretType.Web, gmail.SecretType);
    }

    [Fact]
    public void GivenAFileInAnotherFormat_WhenLoaded_ThenItSaysItCouldNotBeRead()
    {
        // Arrange
        var sut = CreateSut();
        sut.SetFormat(CsvImportDataFormat.Cachy1_x);

        // Act
        sut.LoadFile("export.csv", KeePassCsv);

        // Assert
        Assert.Equal("That file could not be read as Cachy CSV (1.x).", sut.Error);
        Assert.False(sut.CanImport);
    }

    [Fact]
    public void GivenAnEmptyFile_WhenLoaded_ThenItSaysItCouldNotBeRead()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.LoadFile("empty.csv", string.Empty);

        // Assert
        Assert.Equal("That file could not be read as KeePass CSV (1.x).", sut.Error);
        Assert.False(sut.CanImport);
    }

    [Fact]
    public void GivenAFileWithARowMissingItsName_WhenLoaded_ThenItSaysWhatIsMissing_AndNothingCanBeImported()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.LoadFile("export.csv", KeePassCsv + "\"\",\"alice\",\"pw\",\"\",\"\"\r\n");

        // Assert
        Assert.Equal("The secret is missing: Name.", sut.Error);
        Assert.False(sut.CanImport);
    }

    [Fact]
    public void GivenAFileWithOnlyHeaders_WhenLoaded_ThenItSaysNothingWasFound()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.LoadFile("export.csv", "\"Account\",\"Username\",\"Password\",\"Web site\",\"Comments\"\r\n");

        // Assert
        Assert.Equal("No secrets were found in that file.", sut.Error);
        Assert.False(sut.CanImport);
    }

    [Fact]
    public void GivenALoadedFile_WhenTheFormatIsChangedBack_ThenItIsReadAgain()
    {
        // Arrange
        var sut = CreateSut();
        sut.SetFormat(CsvImportDataFormat.Cachy1_x);
        sut.LoadFile("export.csv", KeePassCsv);

        // Act
        sut.SetFormat(CsvImportDataFormat.KeePassCsv1_x);

        // Assert
        Assert.Null(sut.Error);
        Assert.Equal(2, sut.Found.Count);
    }

    [Fact]
    public void GivenAFormatNotOffered_WhenSet_ThenTheFirstIsUsed()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.SetFormat(CsvImportDataFormat.None);

        // Assert
        Assert.Equal(CsvImportDataFormat.KeePassCsv1_x, sut.Format);
    }

    [Fact]
    public async Task GivenALoadedFile_WhenImported_ThenItsSecretsAreAddedOnce()
    {
        // Arrange
        var sut = CreateSut();
        sut.LoadFile("export.csv", KeePassCsv);

        // Act
        var result = await sut.ImportAsync();
        var again = await sut.ImportAsync();

        // Assert
        Assert.True(result);
        Assert.False(again);
        Assert.Equal(2, imported!.Count);
        Assert.Equal("2 secrets imported.", sut.DoneText);
        Assert.False(sut.CanImport);
        mockSession.Verify(x => x.ImportAsync(It.IsAny<IReadOnlyList<Secret>>(), It.IsAny<IProgress<int>?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenImportingFails_WhenImported_ThenTheErrorIsShown()
    {
        // Arrange
        mockSession.Setup(x => x.ImportAsync(It.IsAny<IReadOnlyList<Secret>>(), It.IsAny<IProgress<int>?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ClypseException(ClypseErrorCodes.SecretInvalid, "Name"));
        var sut = CreateSut();
        sut.LoadFile("export.csv", KeePassCsv);

        // Act
        var result = await sut.ImportAsync();

        // Assert
        Assert.False(result);
        Assert.Equal("The secret is missing: Name.", sut.Error);
        Assert.Null(sut.DoneText);
        Assert.True(sut.CanImport);
    }

    private ImportSecretsViewModel CreateSut() => new(mockSession.Object, new CsvSecretsImporterService(), text.Errors, text.Plurals, text.For<ClypseText>());
}
