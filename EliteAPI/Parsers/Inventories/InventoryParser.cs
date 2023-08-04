using EliteAPI.Models.DTOs.Incoming;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Hypixel;

namespace EliteAPI.Parsers.Inventories; 

public static class InventoryParser {
    
    public static void ParseInventory(this ProfileMember member, RawMemberData memberData) {
        member.Api.Inventories = memberData.InventoryContents is not null;
        member.Api.Vault = memberData.PersonalVaultContents is not null;
        
        member.Inventories.ProfileMemberId = member.Id;
        member.Inventories.ProfileMember = member;
        
        member.Inventories.LastUpdated = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        if (memberData.InventoryContents is null) return;
        
        member.Inventories.Armor = memberData.Armor?.Data;
        member.Inventories.EnderChest = memberData.EnderChestContents?.Data;
        member.Inventories.Equipment = memberData.EquipmentContents?.Data;
        member.Inventories.Inventory = memberData.InventoryContents?.Data;
        member.Inventories.TalismanBag = memberData.TalismanBag?.Data;
        member.Inventories.Wardrobe = memberData.WardrobeContents?.Data;

        if (memberData.PersonalVaultContents is not null) {
            member.Inventories.PersonalVault = memberData.PersonalVaultContents.Data;
        }

        member.Inventories.Backpacks = memberData.BackpackContents?.Values.Select(x => x.Data).ToList();
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