using System.Text.Json;
using clypse.core.Blazor;

namespace clypse.core.UnitTests.Blazor;

public class JavaScriptInteropUtilityTests
{
    [Fact]
    public void GivenNullDictionary_WhenGetLongValue_ThenReturnDefaultValue()
    {
        // Arrange
        Dictionary<string, object?>? dictionary = null;
        const string key = "testKey";
        const long defaultValue = 42L;

        // Act
        var result = JavaScriptInteropUtility.GetLongValue(dictionary, key, defaultValue);

        // Assert
        Assert.Equal(defaultValue, result);
    }

    [Fact]
    public void GivenDictionaryWithMissingKey_WhenGetLongValue_ThenReturnDefaultValue()
    {
        // Arrange
        var dictionary = new Dictionary<string, object?> { { "otherKey", 123L } };
        const string key = "testKey";
        const long defaultValue = 42L;

        // Act
        var result = JavaScriptInteropUtility.GetLongValue(dictionary, key, defaultValue);

        // Assert
        Assert.Equal(defaultValue, result);
    }

    [Fact]
    public void GivenDictionaryWithNullValue_WhenGetLongValue_ThenReturnDefaultValue()
    {
        // Arrange
        var dictionary = new Dictionary<string, object?> { { "testKey", null } };
        const string key = "testKey";
        const long defaultValue = 42L;

        // Act
        var result = JavaScriptInteropUtility.GetLongValue(dictionary, key, defaultValue);

        // Assert
        Assert.Equal(defaultValue, result);
    }

    [Theory]
    [InlineData(123L, 123L)]
    [InlineData(456, 456L)]
    [InlineData(789.0, 789L)]
    [InlineData(101.5f, 101L)]
    public void GivenDictionaryWithDirectNumericValue_WhenGetLongValue_ThenReturnConvertedValue(object value, long expected)
    {
        // Arrange
        var dictionary = new Dictionary<string, object?> { { "testKey", value } };
        const string key = "testKey";

        // Act
        var result = JavaScriptInteropUtility.GetLongValue(dictionary, key);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("123", 123L)]
    [InlineData("999", 999L)]
    public void GivenDictionaryWithStringValue_WhenGetLongValue_ThenReturnParsedValue(string value, long expected)
    {
        // Arrange
        var dictionary = new Dictionary<string, object?> { { "testKey", value } };
        const string key = "testKey";

        // Act
        var result = JavaScriptInteropUtility.GetLongValue(dictionary, key);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GivenDictionaryWithInvalidStringValue_WhenGetLongValue_ThenReturnDefaultValue()
    {
        // Arrange
        var dictionary = new Dictionary<string, object?> { { "testKey", "notANumber" } };
        const string key = "testKey";
        const long defaultValue = 42L;

        // Act
        var result = JavaScriptInteropUtility.GetLongValue(dictionary, key, defaultValue);

        // Assert
        Assert.Equal(defaultValue, result);
    }

    [Fact]
    public void GivenDictionaryWithJsonElementNumber_WhenGetLongValue_ThenReturnValue()
    {
        // Arrange
        var jsonDocument = JsonDocument.Parse("{ \"testKey\": 12345 }");
        var jsonElement = jsonDocument.RootElement.GetProperty("testKey");
        var dictionary = new Dictionary<string, object?> { { "testKey", jsonElement } };
        const string key = "testKey";

        // Act
        var result = JavaScriptInteropUtility.GetLongValue(dictionary, key);

        // Assert
        Assert.Equal(12345L, result);
        jsonDocument.Dispose();
    }

    [Fact]
    public void GivenDictionaryWithJsonElementString_WhenGetLongValue_ThenReturnParsedValue()
    {
        // Arrange
        var jsonDocument = JsonDocument.Parse("{ \"testKey\": \"67890\" }");
        var jsonElement = jsonDocument.RootElement.GetProperty("testKey");
        var dictionary = new Dictionary<string, object?> { { "testKey", jsonElement } };
        const string key = "testKey";

        // Act
        var result = JavaScriptInteropUtility.GetLongValue(dictionary, key);

        // Assert
        Assert.Equal(67890L, result);
        jsonDocument.Dispose();
    }

    [Fact]
    public void GivenDictionaryWithJsonElementInvalidString_WhenGetLongValue_ThenReturnDefaultValue()
    {
        // Arrange
        var jsonDocument = JsonDocument.Parse("{ \"testKey\": \"invalidNumber\" }");
        var jsonElement = jsonDocument.RootElement.GetProperty("testKey");
        var dictionary = new Dictionary<string, object?> { { "testKey", jsonElement } };
        const string key = "testKey";
        const long defaultValue = 99L;

        // Act
        var result = JavaScriptInteropUtility.GetLongValue(dictionary, key, defaultValue);

        // Assert
        Assert.Equal(defaultValue, result);
        jsonDocument.Dispose();
    }

    [Fact]
    public void GivenDictionaryWithJsonElementBool_WhenGetLongValue_ThenReturnDefaultValue()
    {
        // Arrange
        var jsonDocument = JsonDocument.Parse("{ \"testKey\": true }");
        var jsonElement = jsonDocument.RootElement.GetProperty("testKey");
        var dictionary = new Dictionary<string, object?> { { "testKey", jsonElement } };
        const string key = "testKey";
        const long defaultValue = 77L;

        // Act
        var result = JavaScriptInteropUtility.GetLongValue(dictionary, key, defaultValue);

        // Assert
        Assert.Equal(defaultValue, result);
        jsonDocument.Dispose();
    }

    [Fact]
    public void GivenNullDictionary_WhenGetIntValue_ThenReturnDefaultValue()
    {
        // Arrange
        Dictionary<string, object?>? dictionary = null;
        const string key = "testKey";
        const int defaultValue = 42;

        // Act
        var result = JavaScriptInteropUtility.GetIntValue(dictionary, key, defaultValue);

        // Assert
        Assert.Equal(defaultValue, result);
    }

    [Fact]
    public void GivenDictionaryWithMissingKey_WhenGetIntValue_ThenReturnDefaultValue()
    {
        // Arrange
        var dictionary = new Dictionary<string, object?> { { "otherKey", 123 } };
        const string key = "testKey";
        const int defaultValue = 42;

        // Act
        var result = JavaScriptInteropUtility.GetIntValue(dictionary, key, defaultValue);

        // Assert
        Assert.Equal(defaultValue, result);
    }

    [Fact]
    public void GivenDictionaryWithNullValue_WhenGetIntValue_ThenReturnDefaultValue()
    {
        // Arrange
        var dictionary = new Dictionary<string, object?> { { "testKey", null } };
        const string key = "testKey";
        const int defaultValue = 42;

        // Act
        var result = JavaScriptInteropUtility.GetIntValue(dictionary, key, defaultValue);

        // Assert
        Assert.Equal(defaultValue, result);
    }

    [Theory]
    [InlineData(123, 123)]
    [InlineData(456L, 456)]
    [InlineData(789.0, 789)]
    [InlineData(101.5f, 101)]
    public void GivenDictionaryWithDirectNumericValue_WhenGetIntValue_ThenReturnConvertedValue(object value, int expected)
    {
        // Arrange
        var dictionary = new Dictionary<string, object?> { { "testKey", value } };
        const string key = "testKey";

        // Act
        var result = JavaScriptInteropUtility.GetIntValue(dictionary, key);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("123", 123)]
    [InlineData("999", 999)]
    public void GivenDictionaryWithStringValue_WhenGetIntValue_ThenReturnParsedValue(string value, int expected)
    {
        // Arrange
        var dictionary = new Dictionary<string, object?> { { "testKey", value } };
        const string key = "testKey";

        // Act
        var result = JavaScriptInteropUtility.GetIntValue(dictionary, key);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GivenDictionaryWithInvalidStringValue_WhenGetIntValue_ThenReturnDefaultValue()
    {
        // Arrange
        var dictionary = new Dictionary<string, object?> { { "testKey", "notANumber" } };
        const string key = "testKey";
        const int defaultValue = 42;

        // Act
        var result = JavaScriptInteropUtility.GetIntValue(dictionary, key, defaultValue);

        // Assert
        Assert.Equal(defaultValue, result);
    }

    [Fact]
    public void GivenDictionaryWithJsonElementNumber_WhenGetIntValue_ThenReturnValue()
    {
        // Arrange
        var jsonDocument = JsonDocument.Parse("{ \"testKey\": 12345 }");
        var jsonElement = jsonDocument.RootElement.GetProperty("testKey");
        var dictionary = new Dictionary<string, object?> { { "testKey", jsonElement } };
        const string key = "testKey";

        // Act
        var result = JavaScriptInteropUtility.GetIntValue(dictionary, key);

        // Assert
        Assert.Equal(12345, result);
        jsonDocument.Dispose();
    }

    [Fact]
    public void GivenDictionaryWithJsonElementString_WhenGetIntValue_ThenReturnParsedValue()
    {
        // Arrange
        var jsonDocument = JsonDocument.Parse("{ \"testKey\": \"67890\" }");
        var jsonElement = jsonDocument.RootElement.GetProperty("testKey");
        var dictionary = new Dictionary<string, object?> { { "testKey", jsonElement } };
        const string key = "testKey";

        // Act
        var result = JavaScriptInteropUtility.GetIntValue(dictionary, key);

        // Assert
        Assert.Equal(67890, result);
        jsonDocument.Dispose();
    }

    [Fact]
    public void GivenDictionaryWithJsonElementInvalidString_WhenGetIntValue_ThenReturnDefaultValue()
    {
        // Arrange
        var jsonDocument = JsonDocument.Parse("{ \"testKey\": \"invalidNumber\" }");
        var jsonElement = jsonDocument.RootElement.GetProperty("testKey");
        var dictionary = new Dictionary<string, object?> { { "testKey", jsonElement } };
        const string key = "testKey";
        const int defaultValue = 99;

        // Act
        var result = JavaScriptInteropUtility.GetIntValue(dictionary, key, defaultValue);

        // Assert
        Assert.Equal(defaultValue, result);
        jsonDocument.Dispose();
    }

    [Fact]
    public void GivenNullDictionary_WhenGetBoolValue_ThenReturnDefaultValue()
    {
        // Arrange
        Dictionary<string, object?>? dictionary = null;
        const string key = "testKey";
        const bool defaultValue = true;

        // Act
        var result = JavaScriptInteropUtility.GetBoolValue(dictionary, key, defaultValue);

        // Assert
        Assert.Equal(defaultValue, result);
    }

    [Fact]
    public void GivenDictionaryWithMissingKey_WhenGetBoolValue_ThenReturnDefaultValue()
    {
        // Arrange
        var dictionary = new Dictionary<string, object?> { { "otherKey", true } };
        const string key = "testKey";
        const bool defaultValue = true;

        // Act
        var result = JavaScriptInteropUtility.GetBoolValue(dictionary, key, defaultValue);

        // Assert
        Assert.Equal(defaultValue, result);
    }

    [Fact]
    public void GivenDictionaryWithNullValue_WhenGetBoolValue_ThenReturnDefaultValue()
    {
        // Arrange
        var dictionary = new Dictionary<string, object?> { { "testKey", null } };
        const string key = "testKey";
        const bool defaultValue = true;

        // Act
        var result = JavaScriptInteropUtility.GetBoolValue(dictionary, key, defaultValue);

        // Assert
        Assert.Equal(defaultValue, result);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void GivenDictionaryWithDirectBoolValue_WhenGetBoolValue_ThenReturnValue(bool value, bool expected)
    {
        // Arrange
        var dictionary = new Dictionary<string, object?> { { "testKey", value } };
        const string key = "testKey";

        // Act
        var result = JavaScriptInteropUtility.GetBoolValue(dictionary, key);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData("True", true)]
    [InlineData("False", false)]
    public void GivenDictionaryWithStringValue_WhenGetBoolValue_ThenReturnParsedValue(string value, bool expected)
    {
        // Arrange
        var dictionary = new Dictionary<string, object?> { { "testKey", value } };
        const string key = "testKey";

        // Act
        var result = JavaScriptInteropUtility.GetBoolValue(dictionary, key);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GivenDictionaryWithInvalidStringValue_WhenGetBoolValue_ThenReturnDefaultValue()
    {
        // Arrange
        var dictionary = new Dictionary<string, object?> { { "testKey", "notABool" } };
        const string key = "testKey";
        const bool defaultValue = true;

        // Act
        var result = JavaScriptInteropUtility.GetBoolValue(dictionary, key, defaultValue);

        // Assert
        Assert.Equal(defaultValue, result);
    }

    [Fact]
    public void GivenDictionaryWithJsonElementTrue_WhenGetBoolValue_ThenReturnTrue()
    {
        // Arrange
        var jsonDocument = JsonDocument.Parse("{ \"testKey\": true }");
        var jsonElement = jsonDocument.RootElement.GetProperty("testKey");
        var dictionary = new Dictionary<string, object?> { { "testKey", jsonElement } };
        const string key = "testKey";

        // Act
        var result = JavaScriptInteropUtility.GetBoolValue(dictionary, key);

        // Assert
        Assert.True(result);
        jsonDocument.Dispose();
    }

    [Fact]
    public void GivenDictionaryWithJsonElementFalse_WhenGetBoolValue_ThenReturnFalse()
    {
        // Arrange
        var jsonDocument = JsonDocument.Parse("{ \"testKey\": false }");
        var jsonElement = jsonDocument.RootElement.GetProperty("testKey");
        var dictionary = new Dictionary<string, object?> { { "testKey", jsonElement } };
        const string key = "testKey";

        // Act
        var result = JavaScriptInteropUtility.GetBoolValue(dictionary, key);

        // Assert
        Assert.False(result);
        jsonDocument.Dispose();
    }

    [Fact]
    public void GivenDictionaryWithJsonElementStringTrue_WhenGetBoolValue_ThenReturnTrue()
    {
        // Arrange
        var jsonDocument = JsonDocument.Parse("{ \"testKey\": \"true\" }");
        var jsonElement = jsonDocument.RootElement.GetProperty("testKey");
        var dictionary = new Dictionary<string, object?> { { "testKey", jsonElement } };
        const string key = "testKey";

        // Act
        var result = JavaScriptInteropUtility.GetBoolValue(dictionary, key);

        // Assert
        Assert.True(result);
        jsonDocument.Dispose();
    }

    [Fact]
    public void GivenDictionaryWithJsonElementStringFalse_WhenGetBoolValue_ThenReturnFalse()
    {
        // Arrange
        var jsonDocument = JsonDocument.Parse("{ \"testKey\": \"false\" }");
        var jsonElement = jsonDocument.RootElement.GetProperty("testKey");
        var dictionary = new Dictionary<string, object?> { { "testKey", jsonElement } };
        const string key = "testKey";

        // Act
        var result = JavaScriptInteropUtility.GetBoolValue(dictionary, key);

        // Assert
        Assert.False(result);
        jsonDocument.Dispose();
    }

    [Fact]
    public void GivenDictionaryWithJsonElementInvalidValue_WhenGetBoolValue_ThenReturnDefaultValue()
    {
        // Arrange
        var jsonDocument = JsonDocument.Parse("{ \"testKey\": 123 }");
        var jsonElement = jsonDocument.RootElement.GetProperty("testKey");
        var dictionary = new Dictionary<string, object?> { { "testKey", jsonElement } };
        const string key = "testKey";
        const bool defaultValue = true;

        // Act
        var result = JavaScriptInteropUtility.GetBoolValue(dictionary, key, defaultValue);

        // Assert
        Assert.Equal(defaultValue, result);
        jsonDocument.Dispose();
    }

    [Fact]
    public void GivenNullDictionary_WhenGetStringValue_ThenReturnDefaultValue()
    {
        // Arrange
        Dictionary<string, object?>? dictionary = null;
        const string key = "testKey";
        const string? defaultValue = "default";

        // Act
        var result = JavaScriptInteropUtility.GetStringValue(dictionary, key, defaultValue);

        // Assert
        Assert.Equal(defaultValue, result);
    }

    [Fact]
    public void GivenDictionaryWithMissingKey_WhenGetStringValue_ThenReturnDefaultValue()
    {
        // Arrange
        var dictionary = new Dictionary<string, object?> { { "otherKey", "value" } };
        const string key = "testKey";
        const string? defaultValue = "default";

        // Act
        var result = JavaScriptInteropUtility.GetStringValue(dictionary, key, defaultValue);

        // Assert
        Assert.Equal(defaultValue, result);
    }

    [Fact]
    public void GivenDictionaryWithNullValue_WhenGetStringValue_ThenReturnDefaultValue()
    {
        // Arrange
        var dictionary = new Dictionary<string, object?> { { "testKey", null } };
        const string key = "testKey";
        const string? defaultValue = "default";

        // Act
        var result = JavaScriptInteropUtility.GetStringValue(dictionary, key, defaultValue);

        // Assert
        Assert.Equal(defaultValue, result);
    }

    [Theory]
    [InlineData("hello", "hello")]
    [InlineData(123, "123")]
    [InlineData(456L, "456")]
    [InlineData(78.9, "78.9")]
    [InlineData(true, "True")]
    [InlineData(false, "False")]
    public void GivenDictionaryWithDirectValue_WhenGetStringValue_ThenReturnStringRepresentation(object value, string expected)
    {
        // Arrange
        var dictionary = new Dictionary<string, object?> { { "testKey", value } };
        const string key = "testKey";

        // Act
        var result = JavaScriptInteropUtility.GetStringValue(dictionary, key);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GivenDictionaryWithJsonElementString_WhenGetStringValue_ThenReturnValue()
    {
        // Arrange
        var jsonDocument = JsonDocument.Parse("{ \"testKey\": \"hello world\" }");
        var jsonElement = jsonDocument.RootElement.GetProperty("testKey");
        var dictionary = new Dictionary<string, object?> { { "testKey", jsonElement } };
        const string key = "testKey";

        // Act
        var result = JavaScriptInteropUtility.GetStringValue(dictionary, key);

        // Assert
        Assert.Equal("hello world", result);
        jsonDocument.Dispose();
    }

    [Fact]
    public void GivenDictionaryWithJsonElementNumber_WhenGetStringValue_ThenReturnStringRepresentation()
    {
        // Arrange
        var jsonDocument = JsonDocument.Parse("{ \"testKey\": 12345 }");
        var jsonElement = jsonDocument.RootElement.GetProperty("testKey");
        var dictionary = new Dictionary<string, object?> { { "testKey", jsonElement } };
        const string key = "testKey";

        // Act
        var result = JavaScriptInteropUtility.GetStringValue(dictionary, key);

        // Assert
        Assert.Equal("12345", result);
        jsonDocument.Dispose();
    }

    [Fact]
    public void GivenDictionaryWithJsonElementTrue_WhenGetStringValue_ThenReturnTrue()
    {
        // Arrange
        var jsonDocument = JsonDocument.Parse("{ \"testKey\": true }");
        var jsonElement = jsonDocument.RootElement.GetProperty("testKey");
        var dictionary = new Dictionary<string, object?> { { "testKey", jsonElement } };
        const string key = "testKey";

        // Act
        var result = JavaScriptInteropUtility.GetStringValue(dictionary, key);

        // Assert
        Assert.Equal("true", result);
        jsonDocument.Dispose();
    }

    [Fact]
    public void GivenDictionaryWithJsonElementFalse_WhenGetStringValue_ThenReturnFalse()
    {
        // Arrange
        var jsonDocument = JsonDocument.Parse("{ \"testKey\": false }");
        var jsonElement = jsonDocument.RootElement.GetProperty("testKey");
        var dictionary = new Dictionary<string, object?> { { "testKey", jsonElement } };
        const string key = "testKey";

        // Act
        var result = JavaScriptInteropUtility.GetStringValue(dictionary, key);

        // Assert
        Assert.Equal("false", result);
        jsonDocument.Dispose();
    }

    [Fact]
    public void GivenDictionaryWithJsonElementNull_WhenGetStringValue_ThenReturnDefaultValue()
    {
        // Arrange
        var jsonDocument = JsonDocument.Parse("{ \"testKey\": null }");
        var jsonElement = jsonDocument.RootElement.GetProperty("testKey");
        var dictionary = new Dictionary<string, object?> { { "testKey", jsonElement } };
        const string key = "testKey";
        const string? defaultValue = "default";

        // Act
        var result = JavaScriptInteropUtility.GetStringValue(dictionary, key, defaultValue);

        // Assert
        Assert.Equal(defaultValue, result);
        jsonDocument.Dispose();
    }

    [Fact]
    public void GivenDictionaryWithJsonElementArray_WhenGetStringValue_ThenReturnDefaultValue()
    {
        // Arrange
        var jsonDocument = JsonDocument.Parse("{ \"testKey\": [1, 2, 3] }");
        var jsonElement = jsonDocument.RootElement.GetProperty("testKey");
        var dictionary = new Dictionary<string, object?> { { "testKey", jsonElement } };
        const string key = "testKey";
        const string? defaultValue = "default";

        // Act
        var result = JavaScriptInteropUtility.GetStringValue(dictionary, key, defaultValue);

        // Assert
        Assert.Equal(defaultValue, result);
        jsonDocument.Dispose();
    }

    [Fact]
    public void GivenDictionaryWithJsonElementObject_WhenGetStringValue_ThenReturnDefaultValue()
    {
        // Arrange
        var jsonDocument = JsonDocument.Parse("{ \"testKey\": { \"nested\": \"value\" } }");
        var jsonElement = jsonDocument.RootElement.GetProperty("testKey");
        var dictionary = new Dictionary<string, object?> { { "testKey", jsonElement } };
        const string key = "testKey";
        const string? defaultValue = "default";

        // Act
        var result = JavaScriptInteropUtility.GetStringValue(dictionary, key, defaultValue);

        // Assert
        Assert.Equal(defaultValue, result);
        jsonDocument.Dispose();
    }

    [Fact]
    public void GivenDefaultValues_WhenCallingMethods_ThenReturnExpectedDefaults()
    {
        // Arrange
        var dictionary = new Dictionary<string, object?>();
        const string key = "missingKey";

        // Act & Assert
        Assert.Equal(0L, JavaScriptInteropUtility.GetLongValue(dictionary, key));
        Assert.Equal(0, JavaScriptInteropUtility.GetIntValue(dictionary, key));
        Assert.False(JavaScriptInteropUtility.GetBoolValue(dictionary, key));
        Assert.Null(JavaScriptInteropUtility.GetStringValue(dictionary, key));
    }

    [Fact]
    public void GivenMissingKey_WhenGetLongValueWithoutDefaultParameter_ThenReturnZero()
    {
        // Arrange
        var dictionary = new Dictionary<string, object?> { { "otherKey", 123L } };
        const string key = "testKey";

        // Act
        var result = JavaScriptInteropUtility.GetLongValue(dictionary, key);

        // Assert
        Assert.Equal(0L, result);
    }

    [Fact]
    public void GivenMissingKey_WhenGetIntValueWithoutDefaultParameter_ThenReturnZero()
    {
        // Arrange
        var dictionary = new Dictionary<string, object?> { { "otherKey", 123 } };
        const string key = "testKey";

        // Act
        var result = JavaScriptInteropUtility.GetIntValue(dictionary, key);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void GivenMissingKey_WhenGetBoolValueWithoutDefaultParameter_ThenReturnFalse()
    {
        // Arrange
        var dictionary = new Dictionary<string, object?> { { "otherKey", true } };
        const string key = "testKey";

        // Act
        var result = JavaScriptInteropUtility.GetBoolValue(dictionary, key);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenMissingKey_WhenGetStringValueWithoutDefaultParameter_ThenReturnNull()
    {
        // Arrange
        var dictionary = new Dictionary<string, object?> { { "otherKey", "value" } };
        const string key = "testKey";

        // Act
        var result = JavaScriptInteropUtility.GetStringValue(dictionary, key);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GivenNullDictionary_WhenGetLongValueWithoutDefaultParameter_ThenReturnZero()
    {
        // Arrange
        Dictionary<string, object?>? dictionary = null;
        const string key = "testKey";

        // Act
        var result = JavaScriptInteropUtility.GetLongValue(dictionary, key);

        // Assert
        Assert.Equal(0L, result);
    }

    [Fact]
    public void GivenNullDictionary_WhenGetIntValueWithoutDefaultParameter_ThenReturnZero()
    {
        // Arrange
        Dictionary<string, object?>? dictionary = null;
        const string key = "testKey";

        // Act
        var result = JavaScriptInteropUtility.GetIntValue(dictionary, key);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void GivenNullDictionary_WhenGetBoolValueWithoutDefaultParameter_ThenReturnFalse()
    {
        // Arrange
        Dictionary<string, object?>? dictionary = null;
        const string key = "testKey";

        // Act
        var result = JavaScriptInteropUtility.GetBoolValue(dictionary, key);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenNullDictionary_WhenGetStringValueWithoutDefaultParameter_ThenReturnNull()
    {
        // Arrange
        Dictionary<string, object?>? dictionary = null;
        const string key = "testKey";

        // Act
        var result = JavaScriptInteropUtility.GetStringValue(dictionary, key);

        // Assert
        Assert.Null(result);
    }
}
