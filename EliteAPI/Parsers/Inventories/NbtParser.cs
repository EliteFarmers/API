using McProtoNet.NBT;
using Microsoft.IdentityModel.Tokens;

namespace EliteAPI.Mappers.Inventories; 

public static class NbtParser {
    public static Dictionary<string, object?> ToJson(this NbtTag tag) {
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
                        ? list.Select(t => t.ToJson()) 
                        : list.Select(t => t.GetValue()));

                break;
            case NbtTagType.Compound:
                if (tag is not NbtCompound compound) return dict;
                dict.Add("value", compound.Select(c => c.ToJson()));
                
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
                NbtTagType.Compound => ((NbtCompound)tag).Select(t => t.ToJson()),
                NbtTagType.IntArray => tag.IntArrayValue,
                NbtTagType.LongArray => tag.LongArrayValue,
                _ => null
            };
        } catch (Exception _) {
            return null;
        }
    } 
}