using Clypse.Portal.Application.Exceptions;
using Clypse.Portal.Application.Services;
using Clypse.Portal.Models.Files;

namespace Clypse.Portal.Application.UnitTests.Services;

public class VaultFileTreeServiceTests
{
    private readonly VaultFileTreeService sut = new();

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    [InlineData("Travel", "Travel")]
    [InlineData("/Travel//Passports/", "Travel/Passports")]
    [InlineData(" Travel / Passports ", "Travel/Passports")]
    public void GivenAPath_WhenNormalized_ThenNamesAreTrimmed_AndEmptyOnesDropped(string? path, string expected)
    {
        // Act
        var normalized = sut.Normalize(path);

        // Assert
        Assert.Equal(expected, normalized);
    }

    [Theory]
    [InlineData("Travel", true)]
    [InlineData(" Old passports ", true)]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("  ", false)]
    [InlineData("Travel/Passports", false)]
    public void GivenAName_WhenChecked_ThenItMustNotBeEmpty_OrContainASlash(string? name, bool expected)
    {
        // Act
        var valid = sut.IsValidName(name);

        // Assert
        Assert.Equal(expected, valid);
    }

    [Theory]
    [InlineData("", "Travel", "Travel")]
    [InlineData(null, "Travel", "Travel")]
    [InlineData("Travel", " Passports ", "Travel/Passports")]
    [InlineData("/Travel/", "Passports", "Travel/Passports")]
    public void GivenAParentAndAName_WhenCombined_ThenThePathIsTheParentsThenTheName(string? parent, string name, string expected)
    {
        // Act
        var path = sut.Combine(parent, name);

        // Assert
        Assert.Equal(expected, path);
    }

    [Theory]
    [InlineData("")]
    [InlineData("a/b")]
    public void GivenAnInvalidName_WhenCombined_ThenThrowsFolderNameInvalid(string name)
    {
        // Act
        var ex = Assert.Throws<ClypseException>(() => sut.Combine("Travel", name));

        // Assert
        Assert.Equal(ClypseErrorCodes.FolderNameInvalid, ex.Code);
    }

    [Theory]
    [InlineData("Travel/Passports/Old", "Travel/Passports", "Old")]
    [InlineData("Travel", "", "Travel")]
    [InlineData("", "", "")]
    [InlineData(null, "", "")]
    public void GivenAPath_WhenSplit_ThenItHasItsParentAndItsName(string? path, string expectedParent, string expectedName)
    {
        // Act
        var parent = sut.GetParent(path);
        var name = sut.GetName(path);

        // Assert
        Assert.Equal(expectedParent, parent);
        Assert.Equal(expectedName, name);
    }

    [Theory]
    [InlineData("Travel", "Travel", true)]
    [InlineData("Travel/Passports", "Travel", true)]
    [InlineData("Travel/Passports/Old", "Travel", true)]
    [InlineData("Travel", "", true)]
    [InlineData("", "", true)]
    [InlineData("Travelling", "Travel", false)]
    [InlineData("Travel", "Travel/Passports", false)]
    [InlineData("travel", "Travel", false)]
    [InlineData("", "Travel", false)]
    public void GivenAPathAndAFolder_WhenChecked_ThenItIsAtOrUnderOnlyByWholeNames(string path, string folder, bool expected)
    {
        // Act
        var under = sut.IsAtOrUnder(path, folder);

        // Assert
        Assert.Equal(expected, under);
    }

    [Theory]
    [InlineData("Travel", "Travel", "Trips", "Trips")]
    [InlineData("Travel/Passports/Old", "Travel", "Trips", "Trips/Passports/Old")]
    [InlineData("Travel/Passports", "Travel/Passports", "Archive/Passports", "Archive/Passports")]
    [InlineData("Travel/Passports", "Travel", "", "Passports")]
    [InlineData("Work", "Travel", "Trips", "Work")]
    public void GivenAPathUnderAFolder_WhenTheFolderMoves_ThenThePathMovesWithIt(string path, string from, string to, string expected)
    {
        // Act
        var rebased = sut.Rebase(path, from, to);

        // Assert
        Assert.Equal(expected, rebased);
    }

    [Fact]
    public void GivenAFileInAFolder_WhenFoldersAreListed_ThenTheFolderShowsWithoutARecord_AndSoDoesEveryFolderAboveIt()
    {
        // Arrange
        var entries = new[] { File("Scan.jpg", "Travel/Passports/Old") };

        // Act
        var folders = sut.GetFolders(entries);

        // Assert
        Assert.Equal(["Travel", "Travel/Passports", "Travel/Passports/Old"], folders);
        Assert.True(sut.Exists(entries, "Travel"));
        Assert.True(sut.Exists(entries, "Travel/Passports"));
        Assert.True(sut.Exists(entries, "Travel/Passports/Old"));
        Assert.False(sut.Exists(entries, "Travel/Visas"));
    }

    [Fact]
    public void GivenAnEmptyFoldersRecord_WhenFoldersAreListed_ThenTheRecordKeepsTheFolder()
    {
        // Arrange
        var entries = new[] { File("Notes.txt", string.Empty), Record("Work/Empty") };

        // Act
        var folders = sut.GetFolders(entries);

        // Assert
        Assert.Equal(["Work", "Work/Empty"], folders);
        Assert.True(sut.Exists(entries, "Work/Empty"));
    }

    [Fact]
    public void GivenNoEntries_WhenChecked_ThenOnlyTheTopLevelExists()
    {
        // Act & Assert
        Assert.Empty(sut.GetFolders([]));
        Assert.True(sut.Exists([], string.Empty));
        Assert.True(sut.Exists([], null));
        Assert.False(sut.Exists([], "Travel"));
    }

    [Fact]
    public void GivenFoldersNamedByFilesAndRecords_WhenListed_ThenEachShowsOnce_InOrder()
    {
        // Arrange
        var entries = new[]
        {
            File("b.txt", "work"),
            File("a.txt", "Travel"),
            Record("Travel"),
            File("c.txt", "Travel/Passports"),
            Record("Archive"),
        };

        // Act
        var folders = sut.GetFolders(entries);

        // Assert
        Assert.Equal(["Archive", "Travel", "Travel/Passports", "work"], folders);
    }

    [Fact]
    public void GivenAFolder_WhenEverythingUnderItIsAsked_ThenItsFilesAndRecordsAtEveryDepthAreReturned()
    {
        // Arrange
        var inside = File("a.jpg", "Travel");
        var deeper = File("b.jpg", "Travel/Passports");
        var record = Record("Travel/Visas");
        var outside = File("c.jpg", "Travelling");
        var top = File("d.jpg", string.Empty);
        var entries = new[] { inside, deeper, record, outside, top };

        // Act
        var under = sut.GetEntriesUnder(entries, "Travel");
        var all = sut.GetEntriesUnder(entries, string.Empty);

        // Assert
        Assert.Equal([inside, deeper, record], under);
        Assert.Equal(entries, all);
    }

    [Fact]
    public void GivenTheTopLevel_WhenViewed_ThenItHasItsFoldersAndFiles_ByName_AndNoBreadcrumb()
    {
        // Arrange
        var entries = new[]
        {
            File("zebra.jpg", string.Empty),
            File("Apple.jpg", string.Empty),
            File("deep.jpg", "Travel/Passports"),
            Record("work"),
            File("inside.jpg", "Travel"),
        };

        // Act
        var view = sut.GetView(entries, null);

        // Assert
        Assert.Equal(string.Empty, view.Path);
        Assert.Empty(view.Breadcrumb);
        Assert.Equal(["Travel", "work"], view.Folders.Select(f => f.Name));
        Assert.Equal(["Travel", "work"], view.Folders.Select(f => f.Path));
        Assert.Equal(["Apple.jpg", "zebra.jpg"], view.Files.Select(f => f.Name));
    }

    [Fact]
    public void GivenAFolder_WhenViewed_ThenItHasItsBreadcrumb_AndOnlyWhatIsDirectlyInIt()
    {
        // Arrange
        var entries = new[]
        {
            File("top.jpg", string.Empty),
            File("b.jpg", "Travel/Passports"),
            File("a.jpg", "Travel/Passports"),
            File("old.jpg", "Travel/Passports/Old/2019"),
            Record("Travel/Passports/Empty"),
            File("visa.jpg", "Travel/Visas"),
        };

        // Act
        var view = sut.GetView(entries, "Travel/Passports");

        // Assert
        Assert.Equal("Travel/Passports", view.Path);
        Assert.Equal(["Travel", "Passports"], view.Breadcrumb.Select(f => f.Name));
        Assert.Equal(["Travel", "Travel/Passports"], view.Breadcrumb.Select(f => f.Path));
        Assert.Equal(["Empty", "Old"], view.Folders.Select(f => f.Name));
        Assert.Equal(["Travel/Passports/Empty", "Travel/Passports/Old"], view.Folders.Select(f => f.Path));
        Assert.Equal(["a.jpg", "b.jpg"], view.Files.Select(f => f.Name));
    }

    [Fact]
    public void GivenAnEmptyFolder_WhenViewed_ThenItsOwnRecordIsNotShownAsAFile()
    {
        // Arrange
        var entries = new[] { Record("Travel") };

        // Act
        var view = sut.GetView(entries, "Travel");

        // Assert
        Assert.Empty(view.Files);
        Assert.Empty(view.Folders);
        Assert.Equal("Travel", Assert.Single(view.Breadcrumb).Path);
    }

    [Fact]
    public void GivenNoEntries_WhenAnyOfThemIsAsked_ThenThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => sut.GetFolders(null!));
        Assert.Throws<ArgumentNullException>(() => sut.Exists(null!, "Travel"));
        Assert.Throws<ArgumentNullException>(() => sut.GetEntriesUnder(null!, "Travel"));
        Assert.Throws<ArgumentNullException>(() => sut.GetView(null!, "Travel"));
    }

    private static VaultFileEntry File(string name, string folder)
    {
        return new VaultFileEntry { Id = Guid.NewGuid().ToString("D"), Kind = VaultFileKind.File, Name = name, Folder = folder };
    }

    private static VaultFileEntry Record(string folder)
    {
        return new VaultFileEntry { Id = Guid.NewGuid().ToString("D"), Kind = VaultFileKind.Folder, Folder = folder };
    }
}
