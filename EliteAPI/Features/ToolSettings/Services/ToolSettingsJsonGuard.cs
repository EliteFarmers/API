using System.Text.Json;
using System.Text.RegularExpressions;
using Ganss.Xss;

namespace EliteAPI.Features.ToolSettings.Services;

public static partial class ToolSettingsJsonGuard
{
	private const int MaxDepth = 8;
	private const int MaxArrayLength = 100;
	public const int MaxRequestBodyBytes = 64 * 1024;

	public static bool TryValidate(JsonElement data, out string? error) {
		error = null;
		return ValidateNode(data, 1, "$", out error);
	}

	public static JsonDocument SanitizeStrings(JsonElement data, HtmlSanitizer sanitizer) {
		var stream = new MemoryStream();
		using var writer = new Utf8JsonWriter(stream);
		WriteSanitizedNode(writer, data, sanitizer);
		writer.Flush();
		stream.Position = 0;
		return JsonDocument.Parse(stream);
	}

	private static bool ValidateNode(JsonElement node, int depth, string path, out string? error) {
		error = null;

		if (depth > MaxDepth) {
			error = $"Maximum JSON depth of {MaxDepth} exceeded at {path}.";
			return false;
		}

		switch (node.ValueKind) {
			case JsonValueKind.Object:
				foreach (var property in node.EnumerateObject()) {
					if (!IsValidKey(property.Name)) {
						error = $"Invalid key '{property.Name}' at {path}. Keys must be alphanumeric only.";
						return false;
					}

					if (!ValidateNode(property.Value, depth + 1, $"{path}.{property.Name}", out error))
						return false;
				}

				return true;

			case JsonValueKind.Array:
				var length = node.GetArrayLength();
				if (length > MaxArrayLength) {
					error = $"Maximum array length of {MaxArrayLength} exceeded at {path}.";
					return false;
				}

				for (var i = 0; i < length; i++) {
					if (!ValidateNode(node[i], depth + 1, $"{path}[{i}]", out error))
						return false;
				}

				return true;

			case JsonValueKind.String:
			case JsonValueKind.Number:
			case JsonValueKind.True:
			case JsonValueKind.False:
			case JsonValueKind.Null:
				return true;

			default:
				error = $"Unsupported JSON token at {path}.";
				return false;
		}
	}

	private static void WriteSanitizedNode(Utf8JsonWriter writer, JsonElement node, HtmlSanitizer sanitizer) {
		switch (node.ValueKind) {
			case JsonValueKind.Object:
				writer.WriteStartObject();
				foreach (var property in node.EnumerateObject()) {
					writer.WritePropertyName(property.Name);
					WriteSanitizedNode(writer, property.Value, sanitizer);
				}

				writer.WriteEndObject();
				return;

			case JsonValueKind.Array:
				writer.WriteStartArray();
				foreach (var item in node.EnumerateArray())
					WriteSanitizedNode(writer, item, sanitizer);
				writer.WriteEndArray();
				return;

			case JsonValueKind.String:
				writer.WriteStringValue(sanitizer.Sanitize(node.GetString() ?? string.Empty));
				return;

			case JsonValueKind.Number:
				writer.WriteRawValue(node.GetRawText(), true);
				return;

			case JsonValueKind.True:
			case JsonValueKind.False:
				writer.WriteBooleanValue(node.GetBoolean());
				return;

			case JsonValueKind.Null:
				writer.WriteNullValue();
				return;

			default:
				writer.WriteNullValue();
				return;
		}
	}

	[GeneratedRegex("^[A-Za-z0-9]+$", RegexOptions.Compiled)]
	private static partial Regex KeyRegex();

	private static bool IsValidKey(string key) => KeyRegex().IsMatch(key);
}
