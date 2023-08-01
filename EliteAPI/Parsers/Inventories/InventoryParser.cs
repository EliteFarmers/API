using EliteAPI.Models.DTOs.Incoming;
using EliteAPI.Models.Entities.Hypixel;
using fNbt;
using Microsoft.IdentityModel.Tokens;

namespace EliteAPI.Parsers.Inventories; 

public static class InventoryParser {
    
    public static void ParseInventory(this ProfileMember member, RawMemberData memberData) {
        member.Api.Inventories = memberData.InventoryContents is not null;
        member.Api.Vault = memberData.PersonalVaultContents is not null;
        
        member.Inventories.ProfileMemberId = member.Id;

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
    
    private static async Task<NbtTag?> DecodeToNbt(this RawInventoryData inventory) {
        if (inventory.Data.IsNullOrEmpty()) return null;
        
        try {
            var decodedInventory = Convert.FromBase64String(inventory.Data);

            await using var compressedStream = new MemoryStream(decodedInventory);

            var file = new NbtFile();
            file.LoadFromStream(compressedStream, NbtCompression.GZip);
            
            return file.RootTag;
        } catch (Exception e) {
            Console.WriteLine(e);
            return null;
        }
    }
}