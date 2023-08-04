using EliteAPI.Config.Settings;
using EliteAPI.Models.Entities.Farming;

namespace EliteAPI.Parsers.Inventories; 

public static class FarmingInventoryParser {
    public static async Task<FarmingInventory> ExtractFarmingItems(this Models.Entities.Hypixel.Inventories inventories) {
        var farming = new FarmingInventory();

        await farming.PopulateFrom(inventories.Inventory);
        await farming.PopulateFrom(inventories.EnderChest);
        await farming.PopulateFrom(inventories.PersonalVault);
        await Task.WhenAll(inventories.Backpacks?.Select(i => farming.PopulateFrom(i)) ?? new List<Task>());
        await farming.PopulateFrom(inventories.Armor);
        await farming.PopulateFrom(inventories.Equipment);

        return farming;
    }
    
    public static async Task PopulateFrom(this FarmingInventory farming, string? inventory) {
        var data = await NbtParser.NbtToItems(inventory);
        if (data is null || data.Count == 0) return;
        
        var toolIds = FarmingItemsConfig.Settings.FarmingToolIds;
        var equipmentIds = FarmingItemsConfig.Settings.FarmingEquipmentIds;
        var armorIds = FarmingItemsConfig.Settings.FarmingArmorIds;
        
        foreach (var item in data) {
            if (item?.SkyblockId is null) continue;

            if (toolIds.ContainsKey(item.SkyblockId)) {
                farming.Tools.Add(item);
            }
            
            if (equipmentIds.ContainsKey(item.SkyblockId)) {
                farming.Equipment.Add(item);
            }
            
            if (armorIds.ContainsKey(item.SkyblockId)) {
                farming.Armor.Add(item);
            }
        }
    }
}