using System.Text.Json;
using HypixelAPI.Networth.Models;

namespace HypixelAPI.Networth.Tests.Infrastructure;

public static class TestHelpers
{
    public static NetworthItem CreateItem(object itemData)
    {
        var json = JsonSerializer.Serialize(itemData);
        var item = JsonSerializer.Deserialize<NetworthItem>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (item == null)
        {
            throw new InvalidOperationException("Failed to deserialize item data.");
        }
        
        // Ensure collections are initialized if null
        item.Calculation ??= new List<NetworthCalculation>();
        item.Attributes ??= new NetworthItemAttributes();
        item.Attributes.Extra ??= new Dictionary<string, object>();
        
        // Fix JsonElement in Extra dictionary
        if (item.Attributes.Extra != null)
        {
            var keys = item.Attributes.Extra.Keys.ToList();
            foreach (var key in keys)
            {
                if (item.Attributes.Extra[key] is JsonElement element)
                {
                    item.Attributes.Extra[key] = GetValue(element);
                }
            }
            if (item.Attributes.Extra.TryGetValue("enchantments", out var enchantmentsObj) && enchantmentsObj is Dictionary<string, object> enchantmentsDict)
            {
                item.Enchantments = enchantmentsDict.ToDictionary(k => k.Key, v => Convert.ToInt32(v.Value));
            }

            if (item.Attributes.Extra.TryGetValue("ability_scroll", out var abilityScrollObj) && abilityScrollObj is List<object> abilityScrollList)
            {
                item.Attributes.AbilityScrolls = abilityScrollList.Select(x => x.ToString()).ToList();
            }

            if (item.Attributes.Extra.TryGetValue("runes", out var runesObj) && runesObj is Dictionary<string, object> runesDict)
            {
                item.Attributes.Runes = runesDict.ToDictionary(k => k.Key, v => Convert.ToInt32(v.Value));
            }

            if (item.Attributes.Extra.TryGetValue("hook", out var hookObj) && hookObj is Dictionary<string, object> hookDict && hookDict.TryGetValue("part", out var hookPart))
            {
                item.Attributes.Hook = new NetworthItemRodPartAttribute { Part = hookPart.ToString() };
            }

            if (item.Attributes.Extra.TryGetValue("line", out var lineObj) && lineObj is Dictionary<string, object> lineDict && lineDict.TryGetValue("part", out var linePart))
            {
                item.Attributes.Line = new NetworthItemRodPartAttribute { Part = linePart.ToString() };
            }

            if (item.Attributes.Extra.TryGetValue("sinker", out var sinkerObj) && sinkerObj is Dictionary<string, object> sinkerDict && sinkerDict.TryGetValue("part", out var sinkerPart))
            {
                item.Attributes.Sinker = new NetworthItemRodPartAttribute { Part = sinkerPart.ToString() };
            }
        }
        
        return item;
    }

    private static object GetValue(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                return element.GetString() ?? string.Empty;
            case JsonValueKind.Number:
                if (element.TryGetInt32(out var i)) return i;
                if (element.TryGetInt64(out var l)) return l;
                return element.GetDouble();
            case JsonValueKind.True:
                return true;
            case JsonValueKind.False:
                return false;
            case JsonValueKind.Array:
                var list = new List<object>();
                foreach (var item in element.EnumerateArray())
                {
                    list.Add(GetValue(item));
                }
                return list;
            case JsonValueKind.Object:
                var dict = new Dictionary<string, object>();
                foreach (var property in element.EnumerateObject())
                {
                    dict[property.Name] = GetValue(property.Value);
                }
                return dict;
            case JsonValueKind.Null:
                return null;
            default:
                return element.ToString(); // Fallback
        }
    }
}
