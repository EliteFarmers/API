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