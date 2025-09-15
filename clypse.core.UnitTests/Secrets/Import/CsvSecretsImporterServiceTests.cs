using clypse.core.Secrets.Import;
using clypse.core.Secrets.Import.Exceptions;

namespace clypse.core.UnitTests.Secrets.Import;

public class CsvSecretsImporterServiceTests
{
    [Fact]
    public void GivenEmptyCsvData_WhenReadDataIsCalled_ThenCsvDataMissingHeadersExceptionIsThrown()
    {
         // Arrange
        var importer = new CsvSecretsImporterService();

        // Act & Assert
        Assert.Throws<CsvDataMissingHeadersException>(() => importer.ReadData(string.Empty));
    }

    [Fact]
    public void GiveCsvData_AndRowsWithMismatchedColumnCounts_WhenReadDataIsCalled_ThenOnlyValidRowsAreImported()
    {
        // Arrange
        var csvData = "Key,Value,Description\n" +
                      "ApiKey,12345,API Key for service\n" +
                      "InvalidRowWithoutEnoughColumns\n" +
                      "DbPassword,passw0rd,Database password,ExtraColumn";
        var importer = new CsvSecretsImporterService();
        var expectedHeaders = csvData.Split('\n')[0].Trim().Split(',');

        // Act
        var count = importer.ReadData(csvData);

        // Assert
        Assert.Equal(1, count);
        Assert.True(expectedHeaders.SequenceEqual(importer.ImportedHeaders));
        var curSecret = importer.ImportedSecrets[0];
        var expectedRow = csvData.Split('\n')[1].Trim().Split(',');
        for (var j = 0; j < expectedHeaders.Length; j++)
        {
            var header = expectedHeaders[j];
            Assert.True(curSecret.ContainsKey(header));
            Assert.Equal(expectedRow[j], curSecret[header]);
        }
    }

    [Fact]
    public void GivenValidCsvData_WhenReadDataIsCalled_ThenSecretsAreImportedSuccessfully()
    {
        // Arrange
        var csvData = "Key,Value,Description\n" +
                      "ApiKey,12345,API Key for service\n" +
                      "DbPassword,passw0rd,Database password";
        var importer = new CsvSecretsImporterService();
        var expectedHeaders = csvData.Split('\n')[0].Trim().Split(',');

        // Act
        var count = importer.ReadData(csvData);

        // Assert
        Assert.Equal(2, count);
        Assert.True(expectedHeaders.SequenceEqual(importer.ImportedHeaders));
        for (var i = 0; i < importer.ImportedSecrets.Count; i++)
        {
            var curSecret = importer.ImportedSecrets[i];
            var expectedRow = csvData.Split('\n')[i + 1].Trim().Split(',');
            for (var j = 0; j < expectedHeaders.Length; j++)
            {
                var header = expectedHeaders[j];
                Assert.True(curSecret.ContainsKey(header));
                Assert.Equal(expectedRow[j], curSecret[header]);
            }
        }
    }
}
