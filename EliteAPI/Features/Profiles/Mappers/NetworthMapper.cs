using EliteAPI.Features.Profiles.Models;
using EliteAPI.Mappers;
using HypixelAPI.Networth.Models;

namespace EliteAPI.Features.Profiles.Mappers;

public static class NetworthMapper
{
    public static NetworthItem ToNetworthItem(this HypixelItem item)
    {
        return item.ToDto().ToNetworthItem();
    }
}
