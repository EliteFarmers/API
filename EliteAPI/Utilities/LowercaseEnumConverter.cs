using System.Text.Json;
using System.Text.Json.Serialization;

namespace EliteAPI.Utilities;

public class LowercaseEnumConverter<TEnum> : JsonConverter<TEnum> where TEnum : struct, Enum
{
    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var enumString = reader.GetString();
        return Enum.TryParse<TEnum>(enumString, ignoreCase: true, out var value) ? value : default;
    }

    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString().ToLowerInvariant());
    }
}