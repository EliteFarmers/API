using EliteAPI.Configuration.Settings;
using EliteAPI.Features.Resources.Auctions.Models;
using FastEndpoints;
using Microsoft.Extensions.Options;

namespace EliteAPI.Features.Resources.Auctions.Services;

[RegisterService<VariantBundleService>(LifeTime.Scoped)]
public class VariantBundleService(IOptions<AuctionHouseSettings> settings)
{
    private readonly AuctionHouseSettings _settings = settings.Value;

    public VariantBundleRequest? ParseBundleKey(string input)
    {
        if (!input.StartsWith("bundle:", StringComparison.OrdinalIgnoreCase)) return null;

        var parts = input.Split(':', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3) return null;

        var category = parts[1].ToLowerInvariant();
        switch (category)
        {
            case VariantBundleCategory.Pet:
                return new VariantBundleRequest(parts[2].ToUpperInvariant(), null, VariantBundleCategory.Pet);
            case VariantBundleCategory.Rune:
                int? level = null;
                if (parts.Length >= 4 && int.TryParse(parts[3], out var parsedLevel)) level = parsedLevel;
                return new VariantBundleRequest(parts[2].ToUpperInvariant(), level, VariantBundleCategory.Rune);
            default:
                return null;
        }
    }

    public bool IsValidBundleId(string skyblockId)
    {
        var allowed = _settings.VariantOnlySkyblockIds ?? [];
        return allowed.Contains(skyblockId, StringComparer.OrdinalIgnoreCase);
    }

    public bool MatchesVariantBundle(string variantKey, VariantBundleRequest bundle)
    {
        if (string.IsNullOrWhiteSpace(variantKey)) return false;
        var variation = AuctionItemVariation.FromKey(variantKey);

        return bundle.Category switch
        {
            VariantBundleCategory.Pet => string.Equals(variation.Pet, bundle.Identifier,
                StringComparison.OrdinalIgnoreCase),
            VariantBundleCategory.Rune => variation.Extra is not null
                                         && variation.Extra.TryGetValue("rune", out var runeSpec)
                                         && IsRuneMatch(runeSpec, bundle.Identifier, bundle.Level),
            _ => false
        };
    }

    private static bool IsRuneMatch(string runeSpec, string targetRune, int? level)
    {
        var split = runeSpec.Split(':', StringSplitOptions.RemoveEmptyEntries);
        if (split.Length != 2) return false;
        if (!string.Equals(split[0], targetRune, StringComparison.OrdinalIgnoreCase)) return false;
        if (!level.HasValue) return true;
        return int.TryParse(split[1], out var runeLevel) && runeLevel == level.Value;
    }
}

public readonly record struct VariantBundleRequest(string Identifier, int? Level, string Category)
{
    public string SkyblockId => Category switch
    {
        VariantBundleCategory.Pet => "PET",
        VariantBundleCategory.Rune => "RUNE",
        _ => string.Empty
    };
}

public static class VariantBundleCategory
{
    public const string Pet = "pet";
    public const string Rune = "rune";
}
