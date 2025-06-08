using EliteAPI.Configuration.Settings;
using EliteAPI.Models.DTOs.Outgoing;
using FastEndpoints;
using Microsoft.Extensions.Options;

namespace EliteAPI.Features.Resources.Auctions.Services;

[RegisterService<VariantKeyGenerator>(LifeTime.Singleton)]
public class VariantKeyGenerator(IOptions<AuctionHouseSettings> settings, ILogger<VariantKeyGenerator> logger)
{
    private readonly List<VariantConfigEntry> _configurations = settings.Value.Variants;
    private const string JoinSeparator = "|";
    private const string DefaultKey = "DEFAULT";
    
    public string? Generate(ItemDto itemDto, string rarity)
    {
        var skyblockId = itemDto.SkyblockId;
        if (string.IsNullOrEmpty(skyblockId))
        {
            logger.LogWarning("Cannot generate variant key: SkyblockId is missing from ItemDto.");
            return null;
        }

        var variantKey = new List<string>();
        if (!settings.Value.DontVaryByRarity.Contains(skyblockId))
        {
            variantKey.Add(rarity.ToUpperInvariant());
        }

        var config = _configurations.FirstOrDefault(c => c.SkyblockId == skyblockId) ??
                     _configurations.FirstOrDefault(c => !string.IsNullOrEmpty(c.SkyblockIdPrefix) && skyblockId.StartsWith(c.SkyblockIdPrefix));

        if (itemDto.PetInfo is not null)
        {
            var petKey = GenerateFromPetLevel(itemDto);
            if (petKey is not null)
            {
                variantKey.Add(petKey);
            }
        }
        
        if (itemDto.ItemAttributes is not null && itemDto.ItemAttributes.Count > 0)
        {
            var attributeKey = GenerateFromItemAttributes(itemDto);
            if (attributeKey is not null)
            {
                variantKey.Add(attributeKey);
            }
        }
        
        if (config is null) return string.Join(JoinSeparator, variantKey);

        try
        {
            return config.Strategy switch
            {
                "ItemAttributes" => GenerateFromItemAttributes(itemDto),
                "PetLevel" => GenerateFromPetLevel(itemDto),
                _ => "DEFAULT_UNKNOWN_STRATEGY"
            };
        }
        catch (System.Exception ex)
        {
            logger.LogError(ex, "Error generating variant key for SkyblockId '{SkyblockId}' with strategy '{Strategy}'.", skyblockId, config.Strategy);
            return "DEFAULT_ERROR";
        }
    }

    private static string? GenerateFromItemAttributes(ItemDto itemDto)
    {
        if (itemDto.ItemAttributes == null || itemDto.ItemAttributes.Count == 0) return null;
        var sortedAttributes = itemDto.ItemAttributes
            .OrderBy(kvp => kvp.Key)
            .Select(kvp => $"{kvp.Key.ToLowerInvariant().Replace(":", "-")}:{kvp.Value.ToString().ToLowerInvariant().Replace(":", "-")}");
        return string.Join(JoinSeparator, sortedAttributes);
    }

    private string? GenerateFromPetLevel(ItemDto itemDto)
    {
        if (itemDto.SkyblockId is null || itemDto.PetInfo is null) return null;
        
        var petLevelGroups = settings.Value.PetLevelGroups;
        
        if (settings.Value.PetLevelGroupOverrides.TryGetValue(itemDto.SkyblockId, out var overrideConfig))
        {
            petLevelGroups = overrideConfig;
        }

        var level = itemDto.PetInfo.Level;
        foreach (var (key, group) in petLevelGroups)
        {
            if (level < group.MinLevel || level > group.MaxLevel) continue;
            return key;
        }

        return null;
    }
}