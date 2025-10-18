namespace EliteAPI.Models.DTOs.Outgoing;

public class InventoriesDto
{
	public string? Inventory { get; set; }
	public string? EnderChest { get; set; }
	public string? Armor { get; set; }
	public string? Wardrobe { get; set; }
	public string? Equipment { get; set; }
	public string? Vault { get; set; }
	public string? Talismans { get; set; }
	public List<string>? Backpacks { get; set; }
}

public class DecodedInventoriesDto
{
	public List<ItemDto?>? Inventory { get; set; }
	public List<ItemDto?>? EnderChest { get; set; }
	public List<ItemDto?>? Armor { get; set; }
	public List<ItemDto?>? Wardrobe { get; set; }
	public List<ItemDto?>? Equipment { get; set; }
	public List<ItemDto?>? Vault { get; set; }
	public List<ItemDto?>? Talismans { get; set; }
	public object? Backpacks { get; set; }
}