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

    [Fact]
    public void GivenValidCsvDataWithLFLineEnding_AndImportedSuccessfully_AndCachy1xCsvImportDataFormat_WhenMapImportedSecretsIsCalled_ThenMappedSecretsAreReturned()
    {
        // Arrange
        var csvData = "Name,Description,Username,Password,Website,Notes,Unmapped\n" +
                      "Hoskins Password,\"Description of secret\",bob@hoskins.com,password123,https://www.hoskins.com,\"Hello World!\",foo\n" +
                      "Boskins Password,\"Description of secret\",hob@boskins.com,password321,https://www.boskins.com,\"Foobar!\",bar\n";
        var importer = new CsvSecretsImporterService();
        importer.ReadData(csvData);
        var csvImportDataFormat = Enums.CsvImportDataFormat.Cachy1_x;
        var fieldMappings = FieldMappings.GetMappingsForCsvImportDataFormat(csvImportDataFormat);

        // Act
        var mappedSecrets = importer.MapImportedSecrets(csvImportDataFormat);

        // Assert
        Assert.Equal(2, mappedSecrets.Count);
        for (var i = 0; i < mappedSecrets.Count; i++)
        {
            var curMappedSecret = mappedSecrets[i];
            var curImportedSecret = importer.ImportedSecrets[i];
            foreach (var curMapping in fieldMappings)
            {
                var sourceField = curMapping.Key;
                var targetField = curMapping.Value;
                Assert.True(curMappedSecret.ContainsKey(targetField));
                Assert.Equal(curImportedSecret[sourceField], curMappedSecret[targetField]);
            }
        }
    }

    [Fact]
    public void GivenValidCsvDataWithCRLFLineEnding_AndImportedSuccessfully_AndCachy1xCsvImportDataFormat_WhenMapImportedSecretsIsCalled_ThenMappedSecretsAreReturned()
    {
        // Arrange
        var csvData = "Name,Description,Username,Password,Website,Notes,Unmapped\r\n" +
                      "Hoskins Password,\"Description of secret\",bob@hoskins.com,password123,https://www.hoskins.com,\"Hello World!\",foo\r\n" +
                      "Boskins Password,\"Description of secret\",hob@boskins.com,password321,https://www.boskins.com,\"Foobar!\",bar\r\n";
        var importer = new CsvSecretsImporterService();
        importer.ReadData(csvData);
        var csvImportDataFormat = Enums.CsvImportDataFormat.Cachy1_x;
        var fieldMappings = FieldMappings.GetMappingsForCsvImportDataFormat(csvImportDataFormat);

        // Act
        var mappedSecrets = importer.MapImportedSecrets(csvImportDataFormat);

        // Assert
        Assert.Equal(2, mappedSecrets.Count);
        for (var i = 0; i < mappedSecrets.Count; i++)
        {
            var curMappedSecret = mappedSecrets[i];
            var curImportedSecret = importer.ImportedSecrets[i];
            foreach (var curMapping in fieldMappings)
            {
                var sourceField = curMapping.Key;
                var targetField = curMapping.Value;
                Assert.True(curMappedSecret.ContainsKey(targetField));
                Assert.Equal(curImportedSecret[sourceField], curMappedSecret[targetField]);
            }
        }
    }
}
