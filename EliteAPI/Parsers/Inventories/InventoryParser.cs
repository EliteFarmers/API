using EliteAPI.Models.DTOs.Incoming;
using EliteAPI.Models.DTOs.Outgoing;

namespace EliteAPI.Mappers.Inventories; 

public static class InventoryParser {
    
    public static void ParseInventory(this Models.Entities.Hypixel.Inventories inventories, RawMemberData memberData) {
        inventories.LastUpdated = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        if (memberData.InventoryContents is null) return;
        
        inventories.Armor = memberData.Armor?.Data;
        inventories.EnderChest = memberData.EnderChestContents?.Data;
        inventories.Equipment = memberData.EquipmentContents?.Data;
        inventories.Inventory = memberData.InventoryContents?.Data;
        inventories.TalismanBag = memberData.TalismanBag?.Data;
        inventories.Wardrobe = memberData.WardrobeContents?.Data;

        if (memberData.PersonalVaultContents is not null) {
            inventories.PersonalVault = memberData.PersonalVaultContents.Data;
        }

        inventories.Backpacks = memberData.BackpackContents?.Values.Select(x => x.Data).ToList();
    }

    public static async Task<DecodedInventoriesDto> DecodeToNbt(this Models.Entities.Hypixel.Inventories inventories) {
        var backpacks = inventories.Backpacks?.Select(async b => await NbtParser.NbtToItems(b)).ToList();
        if (backpacks is not null) {
            await Task.WhenAll(backpacks);
        }
        
        return new DecodedInventoriesDto {
            Armor = await NbtParser.NbtToItems(inventories.Armor),
            EnderChest = await NbtParser.NbtToItems(inventories.EnderChest),
            Equipment = await NbtParser.NbtToItems(inventories.Equipment),
            Inventory = await NbtParser.NbtToItems(inventories.Inventory),
            Vault = await NbtParser.NbtToItems(inventories.PersonalVault),
            Talismans = await NbtParser.NbtToItems(inventories.TalismanBag),
            Wardrobe = await NbtParser.NbtToItems(inventories.Wardrobe),
            Backpacks = backpacks?.Select(b => b.Result).ToList()
        };
    }
}