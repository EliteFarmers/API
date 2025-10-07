using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using EliteAPI.Models.DTOs.Outgoing;

namespace EliteAPI.Features.Profiles.Models;

public class HypixelInventoryDto
{
	public required Guid Id { get; set; }
	
	[MaxLength(64)]
	public required string Name { get; set; }
	
	public List<ItemDto> Items { get; set; } = [];
	
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public Dictionary<string, string>? Metadata { get; set; } 
}