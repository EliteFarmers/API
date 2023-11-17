using EliteAPI.Models.DTOs.Incoming;
using EliteAPI.Models.DTOs.Outgoing;

namespace EliteAPI.Parsers.Inventories; 

public static class InventoryParser {
    
    public static void ParseInventory(this Models.Entities.Hypixel.Inventories inventories, RawMemberData memberData) {
        inventories.LastUpdated = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var incoming = memberData.Inventories;
        
        if (incoming?.InventoryContents is null) return;
        
        inventories.Armor = incoming.Armor?.Data;
        inventories.EnderChest = incoming.EnderChestContents?.Data;
        inventories.Equipment = incoming.EquipmentContents?.Data;
        inventories.Inventory = incoming.InventoryContents?.Data;
        inventories.TalismanBag = incoming.BagContents?.TalismanBag?.Data;
        inventories.Wardrobe = incoming.WardrobeContents?.Data;

        if (incoming.PersonalVaultContents is not null) {
            inventories.PersonalVault = incoming.PersonalVaultContents.Data;
        }

        inventories.Backpacks = incoming.BackpackContents?.Values.Select(x => x.Data).ToList();
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