using System.Text.Json.Serialization;

namespace EliteFarmers.HypixelAPI.DTOs;

public class MuseumResponse
{
	public bool Success { get; set; }
	
	[JsonPropertyName("cause")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public string? Cause { get; set; }
	
	public Dictionary<string, MuseumMember> Members { get; set; } = new();
}

public class MuseumMember
{
	[JsonPropertyName("items")]
	public Dictionary<string, MuseumItem> Items { get; set; } = new();
	
	[JsonPropertyName("special")]
	public List<MuseumItem> Special { get; set; } = [];
	
	[JsonPropertyName("value")]
	public long Value { get; set; }
	
	public bool Appraisal { get; set; }
}

public class MuseumItem
{
	[JsonPropertyName("items")]
	public RawInventoryData Items { get; set; } = new();
	
	[JsonPropertyName("missing")]
	public bool Missing { get; set; }
	
	[JsonPropertyName("donated_as_a_child")]
	public bool DonatedAsAChild { get; set; }
	
	[JsonPropertyName("featured_slot")]
	public string? FeaturedSlot { get; set; }
	
	[JsonPropertyName("borrowing")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public bool Borrowing { get; set; }
	
	[JsonPropertyName("donated_time")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public long DonationTime { get; set; }
}

