namespace clypse.portal.setup.UnitTests.Services.IO;

public class IoServiceTests
{
    [Fact]
    public void GivenIoService_WhenGetApplicationDirectory_ThenReturnsNonEmptyPath()
    {
        // Arrange
        var sut = new clypse.portal.setup.Services.IO.IoService();

        // Act
        var result = sut.GetApplicationDirectory();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.True(Directory.Exists(result));
    }

    [Fact]
    public void GivenIoService_WhenGetApplicationDirectory_ThenReturnsAbsolutePath()
    {
        // Arrange
        var sut = new clypse.portal.setup.Services.IO.IoService();

        // Act
        var result = sut.GetApplicationDirectory();

        // Assert
        Assert.True(Path.IsPathRooted(result), "Application directory should be an absolute path");
    }

    [Fact]
    public void GivenIoService_WhenGetApplicationDirectory_ThenMatchesAppContextBaseDirectory()
    {
        // Arrange
        var sut = new clypse.portal.setup.Services.IO.IoService();

        // Act
        var result = sut.GetApplicationDirectory();

        // Assert
        Assert.Equal(AppContext.BaseDirectory, result);
    }

    [Fact]
    public void GivenIoService_WhenGetCurrentDirectory_ThenReturnsNonEmptyPath()
    {
        // Arrange
        var sut = new clypse.portal.setup.Services.IO.IoService();

        // Act
        var result = sut.GetCurrentDirectory();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.True(Directory.Exists(result));
    }

    [Fact]
    public void GivenIoService_WhenGetApplicationDirectoryAndCurrentDirectory_ThenMayDiffer()
    {
        // Arrange
        var sut = new clypse.portal.setup.Services.IO.IoService();

        // Act
        var appDir = sut.GetApplicationDirectory();
        var currentDir = sut.GetCurrentDirectory();

        // Assert
        // They should both be valid paths, but may or may not be the same
        Assert.NotNull(appDir);
        Assert.NotNull(currentDir);
        Assert.True(Directory.Exists(appDir));
        Assert.True(Directory.Exists(currentDir));
    }
}
