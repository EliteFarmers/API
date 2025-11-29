using System.Text.Json;

namespace HypixelAPI.Networth.Calculators.Helpers;

public static class AttributeHelper
{
	/// <summary>
	/// Safely converts a value from the Extra attributes dictionary to an integer.
	/// Handles both direct integer values and JsonElement objects from deserialization.
	/// </summary>
	public static int ToInt32(object? value) {
		if (value == null) return 0;

		return value switch {
			int i => i,
			long l => (int)l,
			string s when int.TryParse(s, out var parsed) => parsed,
			string s when s.EndsWith("b") && int.TryParse(s.AsSpan(0, s.Length - 1), out var parsedB) => parsedB,
			JsonElement { ValueKind: JsonValueKind.Number } je => je.GetInt32(),
			JsonElement { ValueKind: JsonValueKind.String } je when int.TryParse(je.GetString(), out var parsed) =>
				parsed,
			JsonElement { ValueKind: JsonValueKind.String } je when je.GetString()?.EndsWith("b") == true && int.TryParse(je.GetString()!.AsSpan(0, je.GetString()!.Length - 1), out var parsedB) =>
				parsedB,
			_ => Convert.ToInt32(value)
		};
	}

	/// <summary>
	/// Safely converts a value from the Extra attributes dictionary to a string.
	/// Handles both direct string values and JsonElement objects from deserialization.
	/// </summary>
	public static string? ToString(object? value) {
		if (value == null) return null;

		return value switch {
			string s => s,
			JsonElement { ValueKind: JsonValueKind.String } je => je.GetString(),
			JsonElement je => je.ToString(),
			_ => value.ToString()
		};
	}
}