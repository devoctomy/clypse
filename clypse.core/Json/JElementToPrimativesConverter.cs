using System.Text.Json;
using System.Text.Json.Serialization;

namespace clypse.core.Json;

/// <summary>
/// A custom JSON converter that converts JSON elements to their corresponding primitive types or complex structures.
/// </summary>
public class JElementToPrimativesConverter : JsonConverter<object?>
{
    /// <summary>
    /// Reads and converts the JSON to a .NET object.
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <param name="typeToConvert">The type to convert.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The converted object.</returns>
    public override object? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options) => reader.TokenType switch
        {
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.Number => reader.GetInt32(),
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            JsonTokenType.Null => null,
            JsonTokenType.StartObject => JsonSerializer.Deserialize<Dictionary<string, object?>>(ref reader, options),
            JsonTokenType.StartArray => JsonSerializer.Deserialize<List<object?>>(ref reader, options),
            _ => JsonDocument.ParseValue(ref reader).RootElement.Clone()
        };

    /// <summary>
    /// Writes a .NET object as JSON.
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="value">The value to write.</param>
    /// <param name="options">The serializer options.</param>
    public override void Write(
        Utf8JsonWriter writer,
        object? value,
        JsonSerializerOptions options) =>
        JsonSerializer.Serialize(writer, value, value?.GetType() ?? typeof(object), options);
}
