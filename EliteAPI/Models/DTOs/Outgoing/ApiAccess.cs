using HypixelAPI.DTOs;

namespace EliteAPI.Models.DTOs.Outgoing; 

public class ApiAccessDto {
    public bool Inventories { get; set; } = false;
    public bool Collections { get; set; } = false;
    public bool Skills { get; set; } = false;
    public bool Vault { get; set; } = false;
    // public bool Museum { get; set; } = false; // Don't have a way to get this yet
}

public class UnparsedApiDataDto {
    public Dictionary<string, int>? Perks { get; set; }
    public List<TempStatBuffResponse>? TempStatBuffs { get; set; }
    public object? AccessoryBagSettings { get; set; }
    public object? Bestiary { get; set; }
}