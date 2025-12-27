using System.Text.Json.Serialization;
using EliteFarmers.HypixelAPI.DTOs;

namespace EliteAPI.Models.DTOs.Outgoing;

public class ApiAccessDto
{
	public bool Inventories { get; set; } = false;
	public bool Collections { get; set; } = false;
	public bool Skills { get; set; } = false;
	public bool Vault { get; set; } = false;
	public bool Museum { get; set; } = false;
}

public class UnparsedApiDataDto
{
	public int Copper { get; set; } = 0;
	public Dictionary<string, int> Consumed { get; set; } = new();
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public Dictionary<string, bool>? ExportedCrops { get; set; }
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public int DnaMilestone { get; set; } = 0;
	public Dictionary<string, int> LevelCaps { get; set; } = new();
	public Dictionary<string, int>? Perks { get; set; }
	public List<TempStatBuffResponse>? TempStatBuffs { get; set; }
	public object? AccessoryBagSettings { get; set; }
	public object? Bestiary { get; set; }
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public RawDungeonsResponse? Dungeons { get; set; }
}