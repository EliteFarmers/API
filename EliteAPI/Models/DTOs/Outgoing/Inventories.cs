namespace EliteAPI.Models.DTOs.Outgoing; 

public class InventoriesDto {
    public string? Inventory { get; set; }
    public string? EnderChest { get; set; }
    public string? Armor { get; set; }
    public string? Wardrobe { get; set; }
    public string? Equipment { get; set; }
    public string? PersonalVault { get; set; }
    public string? TalismanBag { get; set; }
    public List<string>? Backpacks { get; set; } 
}

public class DecodedInventoriesDto {
    public object? Inventory { get; set; }
    public object? EnderChest { get; set; }
    public object? Armor { get; set; }
    public object? Wardrobe { get; set; }
    public object? Equipment { get; set; }
    public object? PersonalVault { get; set; }
    public object? TalismanBag { get; set; }
    public List<object>? Backpacks { get; set; } 
}