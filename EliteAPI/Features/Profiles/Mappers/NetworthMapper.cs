using EliteAPI.Features.Profiles.Models;
using EliteAPI.Models.DTOs.Outgoing;
using HypixelAPI.Networth.Models;

namespace EliteAPI.Features.Profiles.Mappers;

public static class NetworthMapper
{
    public static NetworthItem ToNetworthItem(this HypixelItem item)
    {
        var networthItem = new NetworthItem
        {
            SkyblockId = item.SkyblockId,
            Name = item.Name,
            Count = item.Count,
            BasePrice = 0, // Will be calculated
            Enchantments = item.Enchantments ?? new Dictionary<string, int>(),
            Attributes = new NetworthItemAttributes
            {
                Extra = item.Attributes?.Extra ?? new Dictionary<string, object>()
            }
        };

        if (item.Modifier != null)
        {
            networthItem.Attributes.Extra["modifier"] = item.Modifier;
        }

        // Map Gems
        if (item.Gems != null)
        {
            if (!networthItem.Attributes.Extra.ContainsKey("gems"))
            {
                networthItem.Attributes.Extra["gems"] = item.Gems;
            }
        }

        // Map Inventory (for Cake Bag, etc.)
        if (item.Attributes?.Inventory != null)
        {
            networthItem.Attributes.Inventory = item.Attributes.Inventory
                .Where(kv => kv.Value != null)
                .ToDictionary(
                    kv => kv.Key,
                    kv => (NetworthItem?)ToNetworthItem(kv.Value!.ToHypixelItem())
                );
        }

        return networthItem;
    }
}
