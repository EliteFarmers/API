namespace EliteAPI.Models.DTOs.Outgoing; 

public class ItemDto {
    public int Id { get; set; }
    public byte Count { get; set; }
    
    public string? SkyblockId { get; set; }
    public string? Uuid { get; set; }

    public string? Name { get; set; }
    public List<string>? Lore { get; set; }
    
    public Dictionary<string, int>? Enchantments { get; set; }
    public Dictionary<string, string>? Attributes { get; set; }
}