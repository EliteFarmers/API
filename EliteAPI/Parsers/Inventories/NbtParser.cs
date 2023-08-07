using System.IO.Compression;
using EliteAPI.Models.DTOs.Outgoing;
using McProtoNet.NBT;
using Microsoft.IdentityModel.Tokens;

namespace EliteAPI.Mappers.Inventories; 

public static class NbtParser {

    public static async Task<NbtTag?> DecodeNbt(string? data) {
        if (data is null || data.IsNullOrEmpty()) return null;
        
        try {
            var decodedInventory = Convert.FromBase64String(data);

            await using var compressedStream = new MemoryStream(decodedInventory);
            await using var decompressedStream = new GZipStream(compressedStream, CompressionMode.Decompress);

            var reader = new NbtReader(decompressedStream, true);

            return reader.ReadAsTag();
        } catch (Exception e) {
            Console.WriteLine(e);
            return null;
        }
    }
    
    public static async Task<List<ItemDto?>?> NbtToItems(string? data) {
        if (data is null || data.IsNullOrEmpty()) return null;

        var nbt = await DecodeNbt(data);
        if (nbt is null) return null;
        
        if (nbt.TagType != NbtTagType.Compound) return null;
        
        var root = nbt as NbtCompound;
        var inv = root?.FirstOrDefault();
        
        if (inv is not NbtList list) return null;
        
        return list.Select(i => i.ToItem()).Where(i => i is not null).ToList();
    }
    
    public static ItemDto? ToItem(this NbtTag nbtTag) {
        if (nbtTag is not NbtCompound tag) return null;
        
        var item = new ItemDto {
            Id = tag["id"]?.IntValue ?? 0,
            Count = tag["Count"]?.ByteValue ?? 0,
            SkyblockId = tag["tag"]?["ExtraAttributes"]?["id"]?.StringValue,
            Uuid = tag["tag"]?["ExtraAttributes"]?["uuid"]?.StringValue,
            Name = tag["tag"]?["display"]?["Name"]?.StringValue,
            Lore = ((NbtList?) tag["tag"]?["display"]?["Lore"])?
                .Select(l => l.StringValue)
                .ToList(),
            Enchantments = ((NbtCompound?) tag["tag"]?["ExtraAttributes"]?["enchantments"])?
                .Where(e => !e.Name.IsNullOrEmpty() && e.HasValue)
                .Select(e => new KeyValuePair<string, int>(e.Name!, e.IntValue))
                .ToDictionary(x => x.Key, x => x.Value),
            Attributes = ((NbtCompound?) tag["tag"]?["ExtraAttributes"])?
                .Where(e => !e.Name.IsNullOrEmpty() && e.HasValue && e.TagType != NbtTagType.Compound && e.TagType != NbtTagType.List && e.Name != "id" && e.Name != "uuid")
                .Select(e => new KeyValuePair<string, string>(e.Name!, e.GetValue()?.ToString() ?? string.Empty))
                .ToDictionary(x => x.Key, x => x.Value)
        };

        return item;
    }
    
    public static Dictionary<string, object?> ToDictionary(this NbtTag tag) {
        var dict = new Dictionary<string, object?>();

        if (!tag.Name.IsNullOrEmpty()) {
            dict.Add("name", tag.Name);
        }
        
        dict.Add("type", tag.TagType);
        
        switch (tag.TagType) {
            case NbtTagType.List:
                if (tag is not NbtList list) return dict;
                
                if (list.Count == 0) {
                    dict.Add("value", new List<object>());
                    break;
                }
                
                // Check if every item in the list is the same type
                var type = list[0].TagType;
                dict.Add("value",
                    type == NbtTagType.Compound || list.Any(t => t.TagType != type) 
                        ? list.Select(t => t.ToDictionary()) 
                        : list.Select(t => t.GetValue()));

                break;
            case NbtTagType.Compound:
                if (tag is not NbtCompound compound) return dict;
                dict.Add("value", compound.Select(c => c.ToDictionary()));
                
                break;
            default:
                dict.Add("value", tag.GetValue());
                break;
        }
        
        return dict;
    }

    public static object? GetValue(this NbtTag tag) {
        try {
            return tag.TagType switch {
                NbtTagType.Boolean => tag.BoolValue,
                NbtTagType.Short => tag.ShortValue,
                NbtTagType.Int => tag.IntValue,
                NbtTagType.Long => tag.LongValue,
                NbtTagType.Float => tag.FloatValue,
                NbtTagType.Double => tag.DoubleValue,
                NbtTagType.ByteArray => tag.ByteArrayValue,
                NbtTagType.String => tag.StringValue,
                NbtTagType.List => ((NbtList)tag).Select(t => t.GetValue()),
                NbtTagType.Compound => ((NbtCompound)tag).Select(t => t.ToDictionary()),
                NbtTagType.IntArray => tag.IntArrayValue,
                NbtTagType.LongArray => tag.LongArrayValue,
                _ => null
            };
        } catch (Exception _) {
            return null;
        }
    } 
}