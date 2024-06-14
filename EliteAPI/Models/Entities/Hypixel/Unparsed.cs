using System.Text.Json.Nodes;
using HypixelAPI.DTOs;

namespace EliteAPI.Models.Entities.Hypixel; 

public class UnparsedApiData {
    public Dictionary<string, int> Perks { get; set; } = new();
    public List<TempStatBuffResponse> TempStatBuffs { get; set; } = new();
    public JsonObject AccessoryBagSettings { get; set; } = new();
    public JsonObject Bestiary { get; set; } = new();
}