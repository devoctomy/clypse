using System.Globalization;
using Clypse.Portal.Application.Services;
using Clypse.Portal.Application.Text;
using Clypse.Portal.Models.Files;
using Devoctomy.Blazwiz.Testing;

namespace Clypse.Portal.Application.UnitTests.Services;

public sealed class FileTypeServiceTests : IDisposable
{
    private readonly FrameworkText text = new(typeof(ClypseText));
    private readonly CultureInfo culture = CultureInfo.CurrentCulture;

    public FileTypeServiceTests()
    {
        // Sizes are written in the user's culture: fixed here, so the decimal point is known.
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = culture;
    }

    [Fact]
    public void GivenNoText_WhenConstructing_ThenThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Equal("text", Assert.Throws<ArgumentNullException>(() => new FileTypeService(null!)).ParamName);
    }

    [Theory]
    [InlineData("Passport.jpg", "image/jpeg", "image/jpeg")]
    [InlineData("Passport.jpg", "Image/JPEG", "image/jpeg")]
    [InlineData("notes.txt", "text/plain; charset=utf-8", "text/plain")]
    [InlineData("scan.dat", "image/png", "image/png")]
    [InlineData("Passport.JPG", null, "image/jpeg")]
    [InlineData("Passport.jpeg", "", "image/jpeg")]
    [InlineData("photo.png", "  ", "image/png")]
    [InlineData("anim.gif", null, "image/gif")]
    [InlineData("photo.webp", null, "image/webp")]
    [InlineData("notes.txt", null, "text/plain")]
    [InlineData("README.md", null, "text/markdown")]
    [InlineData("README.markdown", "application/octet-stream", "text/markdown")]
    [InlineData("ticket.pdf", "application/octet-stream", "application/pdf")]
    [InlineData("archive.zip", null, "application/octet-stream")]
    [InlineData("noextension", null, "application/octet-stream")]
    [InlineData(null, null, "application/octet-stream")]
    public void GivenAFileBeingAdded_WhenItsTypeIsWorkedOut_ThenWhatTheBrowserSaidIsKept_OrItsNameDecides(string? name, string? reported, string expected)
    {
        // Act
        var type = CreateSut().GetContentType(name, reported);

        // Assert
        Assert.Equal(expected, type);
    }

    [Theory]
    [InlineData("a.jpg", "image/jpeg", FileViewKind.Image)]
    [InlineData("a.png", "image/png", FileViewKind.Image)]
    [InlineData("a.gif", "image/gif", FileViewKind.Image)]
    [InlineData("a.webp", "image/webp", FileViewKind.Image)]
    [InlineData("a.md", "image/png", FileViewKind.Image)]
    [InlineData("a.svg", "image/svg+xml", FileViewKind.None)]
    [InlineData("a.bmp", "image/bmp", FileViewKind.None)]
    [InlineData("a.md", "text/markdown", FileViewKind.Markdown)]
    [InlineData("notes", "text/markdown", FileViewKind.Markdown)]
    [InlineData("a.md", "text/plain", FileViewKind.Markdown)]
    [InlineData("a.markdown", "application/octet-stream", FileViewKind.Markdown)]
    [InlineData("a.md", null, FileViewKind.Markdown)]
    [InlineData("a.txt", "text/plain", FileViewKind.Text)]
    [InlineData("notes", "text/plain", FileViewKind.Text)]
    [InlineData("a.txt", "application/octet-stream", FileViewKind.Text)]
    [InlineData("a.txt", null, FileViewKind.Text)]
    [InlineData("a.pdf", "application/pdf", FileViewKind.None)]
    [InlineData("a.md", "application/pdf", FileViewKind.None)]
    [InlineData("a.html", "text/html", FileViewKind.None)]
    [InlineData("a.zip", null, FileViewKind.None)]
    public void GivenAFile_WhenAskedHowItIsShown_ThenOnlyImagesAndTextAre(string name, string? contentType, FileViewKind expected)
    {
        // Arrange
        var file = new VaultFileEntry { Name = name, ContentType = contentType };

        // Act
        var kind = CreateSut().GetViewKind(file);

        // Assert
        Assert.Equal(expected, kind);
    }

    [Theory]
    [InlineData("a.jpg", "image/jpeg", "bi-file-earmark-image", "Image")]
    [InlineData("a.md", "text/markdown", "bi-file-earmark-richtext", "Markdown document")]
    [InlineData("a.txt", "text/plain", "bi-file-earmark-text", "Text file")]
    [InlineData("a.pdf", "application/pdf", "bi-file-earmark-pdf", "PDF document")]
    [InlineData("a.pdf", null, "bi-file-earmark-pdf", "PDF document")]
    [InlineData("a.zip", "application/zip", "bi-file-earmark", "File")]
    public void GivenAFile_WhenDescribed_ThenItHasItsTypesIconAndName(string name, string? contentType, string expectedIcon, string expectedName)
    {
        // Arrange
        var sut = CreateSut();
        var file = new VaultFileEntry { Name = name, ContentType = contentType };

        // Act
        var icon = sut.GetIcon(file);
        var typeName = sut.GetTypeName(file);

        // Assert
        Assert.Equal(expectedIcon, icon);
        Assert.Equal(expectedName, typeName);
    }

    [Theory]
    [InlineData(-5, "0 B")]
    [InlineData(0, "0 B")]
    [InlineData(1023, "1023 B")]
    [InlineData(1024, "1 KB")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(2481133, "2.4 MB")]
    [InlineData(20L * 1024 * 1024, "20 MB")]
    [InlineData(3L * 1024 * 1024 * 1024, "3 GB")]
    [InlineData(5000L * 1024 * 1024 * 1024, "5000 GB")]
    public void GivenASize_WhenDescribed_ThenItIsInTheLargestUnitThatFits(long bytes, string expected)
    {
        // Act
        var size = CreateSut().FormatSize(bytes);

        // Assert
        Assert.Equal(expected, size);
    }

    [Fact]
    public void GivenNoFile_WhenAsked_ThenThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => CreateSut().GetViewKind(null!));
    }

    private FileTypeService CreateSut() => new(text.For<ClypseText>());
}
