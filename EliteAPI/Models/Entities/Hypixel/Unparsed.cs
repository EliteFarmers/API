using System.Text.Json.Nodes;
using EliteAPI.Models.DTOs.Incoming;

namespace EliteAPI.Models.Entities.Hypixel; 

public class UnparsedApiData {
    public Dictionary<string, int> Perks { get; set; } = new();
    public List<TempStatBuff> TempStatBuffs { get; set; } = new();
    public JsonObject AccessoryBagSettings { get; set; } = new();
}