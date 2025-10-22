using System.Text.Json.Serialization;

namespace EliteFarmers.HypixelAPI.DTOs;

public class FiresalesResponse
{
	public bool Success { get; set; }
	public List<FiresaleItem> Sales { get; set; } = [];
}

public class FiresaleItem
{
	[JsonPropertyName("item_id")] public required string ItemId { get; set; }

	/// <summary>
	/// Unix milliseconds
	/// </summary>
	public long Start { get; set; }

	/// <summary>
	/// Unix milliseconds
	///	</summary>
	public long End { get; set; }

	public int Amount { get; set; }
	public int Price { get; set; }
}