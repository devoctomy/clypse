using clypse.core.Vault;

namespace clypse.core.UnitTests.Json;

public class JElementToPrimativesConverterTests
{
    [Fact]
    public void GivenManifestJson_WhenDeserialise_ThenPropertiesNotJsonWankery()
    {
        // Arrange
        var manifest = new VaultManifest
        {
            ClypseCoreVersion = "1.0.0",
            CompressionServiceName = "TestCompressionService",
            CryptoServiceName = "TestCryptoService",
            EncryptedCloudStorageProviderName = "TestEncryptedCloudStorageProvider",
            Parameters = new Dictionary<string, object>
            {
                { "ParamInt", 10000 },
                { "ParamString", "SomeValue" },
            },
        };
        var vaultManifestJson = System.Text.Json.JsonSerializer.Serialize(manifest);

        var sut = new core.Json.JElementToPrimativesConverter();
        var options = new System.Text.Json.JsonSerializerOptions
        {
            Converters =
            {
                sut,
            },
        };

        // Act
        var deserialised = System.Text.Json.JsonSerializer.Deserialize<VaultManifest>(vaultManifestJson, options);

        // Assert
        Assert.IsType<int>(deserialised!.Parameters["ParamInt"]);
        Assert.IsType<string>(deserialised!.Parameters["ParamString"]);
    }
}
