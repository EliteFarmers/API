using System.Text.Json.Nodes;
using EliteFarmers.HypixelAPI.DTOs;

namespace EliteAPI.Models.Entities.Hypixel;

public class UnparsedApiData
{
	public int Copper { get; set; } = 0;
	public Dictionary<string, int> Consumed { get; set; } = new();
	public Dictionary<string, int> LevelCaps { get; set; } = new();
	public Dictionary<string, int> Perks { get; set; } = new();
	public List<TempStatBuffResponse> TempStatBuffs { get; set; } = new();
	public JsonObject AccessoryBagSettings { get; set; } = new();
	public JsonObject Bestiary { get; set; } = new();
}