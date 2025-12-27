using System.Text.Json.Nodes;
using EliteFarmers.HypixelAPI.DTOs;

namespace EliteAPI.Models.Entities.Hypixel;

public class UnparsedApiData
{
	public int Copper { get; set; } = 0;
	public Dictionary<string, int> Consumed { get; set; } = new();
	public Dictionary<string, bool>? ExportedCrops { get; set; }
	public int DnaMilestone { get; set; } = 0;
	public Dictionary<string, int> LevelCaps { get; set; } = new();
	public Dictionary<string, int> Perks { get; set; } = new();
	public List<TempStatBuffResponse> TempStatBuffs { get; set; } = new();
	public RawAccessoryBagStorage AccessoryBagSettings { get; set; } = new();
	public RawBestiaryResponse Bestiary { get; set; } = new();
	public RawDungeonsResponse Dungeons {  get; set; } = new();
	public Dictionary<string, int> Essence { get; set; } = new();
}