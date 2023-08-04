using EliteAPI.Models.Entities.Farming;

namespace EliteAPI.Parsers.Inventories; 

public static class FarmingInventoryParser {
    
    public static FarmingInventory ExtractFarmingItems(this Models.Entities.Hypixel.Inventories inventories) {
        var farming = new FarmingInventory();

        
        
        
        
        
        return farming;
    }
    
    public static async void PopulateFrom(this FarmingInventory farming, string? inventory) {
        var data = await NbtParser.NbtToItems(inventory);
        if (data is null) return;
        
        
        
        
        
    }
}