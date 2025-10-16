using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using EliteAPI.Models.DTOs.Outgoing;

namespace EliteAPI.Features.Profiles.Models;

public class HypixelInventoryDto
{
	public required Guid Id { get; set; }

	[MaxLength(64)] public required string Name { get; set; }

	/// <summary>
	/// Dictionary of slot to item mapping, null if the slot is empty
	/// </summary>
	public Dictionary<string, ItemDto?> Items { get; set; } = [];

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public Dictionary<string, string>? Metadata { get; set; }
}