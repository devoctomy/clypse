using System.Text;
using System.Text.Json;
using clypse.core.Vault;
using clypse.core.Json;

namespace clypse.core.UnitTests.Json;

public class JElementToPrimativesConverterTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        Converters = { new JElementToPrimativesConverter() }
    };

    private static object? ReadValue(string json, bool advanceReader = true)
    {
        var converter = new JElementToPrimativesConverter();
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));

        if (advanceReader)
        {
            Assert.True(reader.Read());
        }

        return converter.Read(ref reader, typeof(object), SerializerOptions);
    }

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
        var vaultManifestJson = JsonSerializer.Serialize(manifest);

        var sut = new JElementToPrimativesConverter();
        var options = new JsonSerializerOptions
        {
            Converters =
            {
                sut,
            },
        };

        // Act
        var deserialised = JsonSerializer.Deserialize<VaultManifest>(vaultManifestJson, options);

        // Assert
        Assert.IsType<int>(deserialised!.Parameters["ParamInt"]);
        Assert.IsType<string>(deserialised!.Parameters["ParamString"]);
    }

    [Fact]
    public void GivenTokenTrue_WhenRead_ThenReturnsTrue()
    {
        // Arrange & Act
        var result = ReadValue("true");

        // Assert
        var value = Assert.IsType<bool>(result);
        Assert.True(value);
    }

    [Fact]
    public void GivenTokenFalse_WhenRead_ThenReturnsFalse()
    {
        // Arrange & Act
        var result = ReadValue("false");

        // Assert
        var value = Assert.IsType<bool>(result);
        Assert.False(value);
    }

    [Fact]
    public void GivenTokenNull_WhenRead_ThenReturnsNull()
    {
        // Arrange & Act
        var result = ReadValue("null");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GivenTokenStartObject_WhenRead_ThenReturnsDictionary()
    {
        // Arrange & Act
        var result = ReadValue("{\"name\":\"clypse\",\"value\":1}");

        // Assert
        var dictionary = Assert.IsType<Dictionary<string, object?>>(result);
        Assert.Equal("clypse", dictionary["name"]);
        Assert.Equal(1, Assert.IsType<int>(dictionary["value"]));
    }

    [Fact]
    public void GivenTokenStartArray_WhenRead_ThenReturnsList()
    {
        // Arrange & Act
        var result = ReadValue("[true,false,2]");

        // Assert
        var list = Assert.IsType<List<object?>>(result);
        Assert.Collection(
            list,
            item => Assert.True(Assert.IsType<bool>(item)),
            item => Assert.False(Assert.IsType<bool>(item)),
            item => Assert.Equal(2, Assert.IsType<int>(item)));
    }

    [Fact]
    public void GivenTokenDefault_WhenRead_ThenReturnsJsonElement()
    {
        // Arrange & Act
        var result = ReadValue("{\"number\":5}", advanceReader: false);

        // Assert
        var element = Assert.IsType<JsonElement>(result);
        Assert.Equal(5, element.GetProperty("number").GetInt32());
    }
}
