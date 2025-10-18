using System.Text.Json;
using System.Text.Json.Serialization;

namespace EliteAPI.Mappers.Converters;

public class RabbitDictionaryConverter : JsonConverter<Dictionary<string, int>>
{
	public override Dictionary<string, int> Read(ref Utf8JsonReader reader, Type typeToConvert,
		JsonSerializerOptions options) {
		var dictionary = new Dictionary<string, int>();

		if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException("Expected StartObject token");

		while (reader.Read()) {
			if (reader.TokenType == JsonTokenType.EndObject) return dictionary;

			if (reader.TokenType != JsonTokenType.PropertyName) throw new JsonException("Expected PropertyName token");

			var key = reader.GetString();

			reader.Read();

			if (key is not null && reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var value))
				dictionary[key] = value;
			else
				// Skip any value that is not an integer
				reader.TrySkip();
		}

		throw new JsonException("Unexpected end of JSON object");
	}

	public override void Write(Utf8JsonWriter writer, Dictionary<string, int> value, JsonSerializerOptions options) {
		writer.WriteStartObject();

		foreach (var kvp in value) {
			writer.WriteNumber(kvp.Key, kvp.Value);
		}

		writer.WriteEndObject();
	}
}