using EliteAPI.Config.Settings;
using EliteAPI.Models.DTOs.Incoming;
using EliteAPI.Models.Entities.Farming;
using EliteAPI.Models.Entities.Hypixel;

namespace EliteAPI.Parsers.Inventories; 

public static class FarmingInventoryParser {
    public static async Task<FarmingInventory> ExtractFarmingItems(this RawMemberData memberData, ProfileMember member) {
        var farming = new FarmingInventory();
        
        member.Api.Inventories = memberData.InventoryContents is not null;
        member.Api.Vault = memberData.PersonalVaultContents is not null;
        
        if (memberData.InventoryContents is null) return farming;
        
        await farming.PopulateFrom(memberData.InventoryContents?.Data);
        await farming.PopulateFrom(memberData.EnderChestContents?.Data);
        await farming.PopulateFrom(memberData.PersonalVaultContents?.Data);
        await Task.WhenAll(memberData.BackpackContents?.Values.Select(i => farming.PopulateFrom(i.Data)) ?? new List<Task>());
        await farming.PopulateFrom(memberData.Armor?.Data);
        await farming.PopulateFrom(memberData.WardrobeContents?.Data);
        await farming.PopulateFrom(memberData.EquipmentContents?.Data);

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