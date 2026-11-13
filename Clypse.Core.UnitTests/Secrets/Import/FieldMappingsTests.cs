using Clypse.Core.Enums;
using Clypse.Core.Secrets.Import;

namespace Clypse.Core.UnitTests.Secrets.Import;

public class FieldMappingsTests
{
    [Fact]
    public void GivenKeePassFormat_WhenGetMappingsForCsvImportDataFormat_ThenKeePassColumnsAreMapped()
    {
        // Act
        var result = FieldMappings.GetMappingsForCsvImportDataFormat(CsvImportDataFormat.KeePassCsv1_x);

        // Assert
        Assert.Equal("Name", result["Account"]);
        Assert.Equal("UserName", result["Username"]);
        Assert.Equal("Password", result["Password"]);
        Assert.Equal("WebsiteUrl", result["Web site"]);
        Assert.Equal("Comments", result["Comments"]);
    }

    [Fact]
    public void GivenNoFormat_WhenGetMappingsForCsvImportDataFormat_ThenArgumentOutOfRangeExceptionThrown()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => FieldMappings.GetMappingsForCsvImportDataFormat(CsvImportDataFormat.None));
    }
}
