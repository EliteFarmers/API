using NJsonSchema;
using NJsonSchema.Generation;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EliteAPI.Utilities;

/// <summary>
/// When applied to an enum, serializes its values as camelCase strings.
/// </summary>
public class JsonStringEnumAttribute : JsonConverterAttribute
{
	public override JsonConverter CreateConverter(Type typeToConvert) {
		return new JsonStringEnumConverter(JsonNamingPolicy.CamelCase);
	}
}

/// <summary>
/// This processor finds enums with the [JsonStringEnum] attribute
/// and updates the swagger schema to represent them as camelCase strings.
/// </summary>
public class EnumAttributeSchemaProcessor : ISchemaProcessor
{
	public void Process(SchemaProcessorContext context) {
		var type = context.ContextualType.Type;

		if (!type.IsEnum || !type.IsDefined(typeof(JsonStringEnumAttribute), false)) return;

		var schema = context.Schema;
		schema.Type = JsonObjectType.String; // Set schema type to string
		schema.Format = null; // Clear any integer formats
		schema.Enumeration.Clear();
		schema.EnumerationNames.Clear(); // Clear out the default integer values

		// Add the string values
		foreach (var name in Enum.GetNames(type)) {
			schema.Enumeration.Add(JsonNamingPolicy.CamelCase.ConvertName(name));
		}
	}
}