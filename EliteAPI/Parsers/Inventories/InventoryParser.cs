using System.IO.Compression;
using System.Text.Json;
using EliteAPI.Models.DTOs.Incoming;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Hypixel;
using Microsoft.IdentityModel.Tokens;
using SharpNBT;

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
        return new DecodedInventoriesDto {
            Armor = await DecodeToNbt(inventories.Armor),
            EnderChest = await DecodeToNbt(inventories.EnderChest),
            Equipment = await DecodeToNbt(inventories.Equipment),
            Inventory = await DecodeToNbt(inventories.Inventory),
            PersonalVault = await DecodeToNbt(inventories.PersonalVault),
            TalismanBag = await DecodeToNbt(inventories.TalismanBag),
            Wardrobe = await DecodeToNbt(inventories.Wardrobe),
            Backpacks = inventories.Backpacks?.Select(async b => await DecodeToNbt(b)).ToList<object>()
        };
    }

    private static async Task<object?> DecodeToNbt(string? data) {
        if (data is null || data.IsNullOrEmpty()) return null;
        
        try {
            var decodedInventory = Convert.FromBase64String(data);

            await using var compressedStream = new MemoryStream(decodedInventory);

            await using var reader = new TagReader(new GZipStream(compressedStream, CompressionMode.Decompress),
                FormatOptions.Java);

            var tag = reader.ReadTag<CompoundTag>();
            var stringTag = tag.ToJsonString();
            return JsonSerializer.Deserialize<object>(stringTag);
        } catch (Exception e) {
            Console.WriteLine(e);
            return null;
        }
    }
}