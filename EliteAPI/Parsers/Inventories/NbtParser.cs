using System.IO.Compression;
using System.Text.Json;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Utilities;
using McProtoNet.NBT;
using ZLinq;

namespace EliteAPI.Parsers.Inventories; 

public static class NbtParser {

    public static async Task<NbtTag?> DecodeNbt(string? data) {
        if (data is null || string.IsNullOrEmpty(data)) return null;
        
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
        
        return list.AsValueEnumerable().Select(i => i.ToItem()).Where(i => i is not null).ToList();
    }
    
    public static async Task<ItemDto?> NbtToItem(string? itemData) {
        if (itemData is null || itemData.IsNullOrEmpty()) return null;

        var nbt = await DecodeNbt(itemData);
        if (nbt is null) return null;
        
        if (nbt.TagType != NbtTagType.Compound) return null;
        
        var root = nbt as NbtCompound;
        var item = root?.FirstOrDefault();

        if (item is not NbtList list) return null;
        
        return list.FirstOrDefault()?.ToItem();
    }
    
    public static ItemDto? ToItem(this NbtTag nbtTag) {
        if (nbtTag is not NbtCompound tag) return null;

        var extraAttributes = tag["tag"]?["ExtraAttributes"];
        var skyblockId = extraAttributes?["id"]?.StringValue;
        var petInfo = extraAttributes?["petInfo"]?.StringValue;
        
        var item = new ItemDto {
            Id = tag["id"]?.IntValue ?? 0,
            Count = tag["Count"]?.ByteValue ?? 0,
            SkyblockId = skyblockId,
            Uuid = extraAttributes?["uuid"]?.StringValue,
            Name = tag["tag"]?["display"]?["Name"]?.StringValue,
            Lore = ((NbtList?) tag["tag"]?["display"]?["Lore"])?
                .AsValueEnumerable()
                .Select(l => l.StringValue)
                .ToList(),
            Enchantments = ((NbtCompound?) extraAttributes?["enchantments"])?
                .AsValueEnumerable()
                .Where(e => !e.Name.IsNullOrEmpty() && e.HasValue)
                .Select(e => new KeyValuePair<string, int>(e.Name!, e.IntValue))
                .ToDictionary(x => x.Key, x => x.Value),
            Attributes = ((NbtCompound?) extraAttributes)?
                .AsValueEnumerable()
                .Where(e => !e.IsSimpleType() && e.Name != "id" && e.Name != "uuid" && e.Name != "petInfo")
                .Select(e => new KeyValuePair<string, string>(e.Name!, e.GetValue()?.ToString() ?? string.Empty))
                .ToDictionary(x => x.Key, x => x.Value),
            ItemAttributes = ((NbtCompound?) extraAttributes?["attributes"])?
                .AsValueEnumerable()
                .Where(e => !e.IsSimpleType())
                .Select(e => new KeyValuePair<string, string>(e.Name!, e.GetValue()?.ToString() ?? string.Empty))
                .ToDictionary(x => x.Key, x => x.Value),
            Gems = ((NbtCompound?) extraAttributes?["gems"])?
                .AsValueEnumerable()
                .Where(e => !e.Name.IsNullOrEmpty() && (e is { TagType: NbtTagType.String, HasValue: true } || (e.TagType == NbtTagType.Compound && e["quality"]?.HasValue is true)))
                .Select(e => e.TagType != NbtTagType.Compound 
                    ? new KeyValuePair<string, string>(e.Name!, e.GetValue()?.ToString() ?? string.Empty) 
                    : new KeyValuePair<string, string>(e.Name!, e["quality"]?.GetValue()?.ToString() ?? string.Empty))
                .ToDictionary(x => x.Key, x => x.Value)
        };

        if (petInfo is not null) {
            try {
                var info = JsonSerializer.Deserialize<ItemPetInfoDto>(petInfo);
                if (info is null) return item;
                
                info.Level = info.GetLevel();
                item.PetInfo = info;
            } catch {
                // ignored
            }
        }

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
        } catch (Exception) {
            return null;
        }
    }

    public static bool IsSimpleType(this NbtTag tag)
    {
        return !tag.Name.IsNullOrEmpty() && tag.HasValue 
            && tag.TagType != NbtTagType.Compound 
            && tag.TagType != NbtTagType.List;
    }
}