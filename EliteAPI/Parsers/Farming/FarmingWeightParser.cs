using System.Text.Json;
using EliteAPI.Configuration.Settings;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Models.Entities.Timescale;
using EliteAPI.Parsers.Inventories;
using EliteAPI.Utilities;
using HypixelAPI.DTOs;

namespace EliteAPI.Parsers.Farming;

public static class FarmingWeightParser
{
    public static async Task ParseFarmingWeight(this ProfileMember member, Dictionary<string, int> craftedMinions, ProfileMemberResponse memberData)
    {
        member.Farming.ProfileMemberId = member.Id;
        member.Farming.ProfileMember = member;

        member.Farming.UncountedCrops = new Dictionary<Crop, long>();
        member.ParsePests(memberData);

        member.Farming.CropWeight = ParseCropWeight(member.Collections, member.Farming.UncountedCrops);
        member.Farming.BonusWeight = ParseBonusWeight(member, craftedMinions);

        member.Farming.TotalWeight = member.Farming.CropWeight.Sum(x => x.Value) 
                                           + member.Farming.BonusWeight.Sum(x => x.Value);

        member.Farming.Inventory = await memberData.ExtractFarmingItems(member);
        
    }

    public static Dictionary<string, double> ParseCropWeight(JsonDocument jsonCollections, Dictionary<Crop, long> uncounted) {
        var collections = jsonCollections.Deserialize<Dictionary<string, long>>() ?? new Dictionary<string, long>();
        return ParseCropWeight(collections, uncounted);
    }

    public static Dictionary<string, double> ParseCropWeight(Dictionary<string, long> collections, Dictionary<Crop, long> uncounted) {
        var crops = new Dictionary<string, double>();

        var collectionPerWeight = FarmingWeightConfig.Settings.CropsPerOneWeight;

        foreach (var cropId in FarmingWeightConfig.Settings.CropItemIds)
        {
            var formattedName = FormatUtils.GetFormattedCropName(cropId);
            if (formattedName is null) continue;

            collections.TryGetValue(cropId, out var amount);
            collectionPerWeight.TryGetValue(cropId.Replace(':', '_'), out var perWeight);

            if (perWeight == 0 || amount == 0)
            {
                crops.Add(formattedName, 0);
                continue;
            }
            
            // Subtract uncounted crops from the total amount
            var crop = FormatUtils.GetCropFromItemId(cropId);
            if (crop.HasValue && uncounted.TryGetValue(crop.Value, out var uncountedAmount)) {
                amount = Math.Max(0, amount - uncountedAmount);
            }

            var weight = (double) amount / perWeight;

            crops.Add(formattedName, weight);
        }

        // Mushroom is a special case, it needs to be calculated dynamically based on the
        // ratio between the farmed crops that give two mushrooms per break with cow pet
        // and the farmed crops that give one mushroom per break with cow pet
        if (!collections.TryGetValue("MUSHROOM_COLLECTION", out var mushroomAmount)) return crops;

        var mushroomPerWeight = collectionPerWeight["MUSHROOM_COLLECTION"];

        var totalWeight = crops.Sum(x => x.Value);
        var doubleBreakCrops = crops["Cactus"] + crops["Sugar Cane"];

        var doubleBreakRatio = doubleBreakCrops / totalWeight;
        var normalCropRatio = (totalWeight - doubleBreakCrops) / totalWeight;

        var mushroomWeight = doubleBreakRatio * ((double) mushroomAmount / (mushroomPerWeight * 2)) +
                             normalCropRatio * ((double) mushroomAmount / mushroomPerWeight);

        // Check if mushroomWeight is NaN (can happen if totalWeight is 0)
        if (double.IsNaN(mushroomWeight)) mushroomWeight = 0;

        crops["Mushroom"] = mushroomWeight;

        return crops;
    }
    
    public static Dictionary<Crop, double> ParseCropWeight(this Dictionary<Crop, long> collections, Dictionary<Crop, double>? weights = null, bool forEvent = false) {
        var crops = new Dictionary<Crop, double>();

        var collectionPerWeight = forEvent
            ? FarmingWeightConfig.Settings.EventCropsPerOneWeight
            : FarmingWeightConfig.Settings.CropsPerOneWeight;

        foreach (var cropId in FarmingWeightConfig.Settings.CropItemIds)
        {
            var formattedName = FormatUtils.GetFormattedCropName(cropId);
            var crop = FormatUtils.FormattedCropNameToCrop(formattedName ?? "");
            if (formattedName is null || !crop.HasValue) continue;

            collections.TryGetValue(crop.Value, out var amount);
            
            collectionPerWeight.TryGetValue(cropId.Replace(':', '_'), out var perWeight);

            // Use the provided weight if it exists
            if (weights is not null && weights.TryGetValue(crop.Value, out var overrideWeight))
            {
                perWeight = overrideWeight;
            }
            
            if (crops.ContainsKey(crop.Value)) continue;

            if (perWeight == 0 || amount == 0)
            {
                crops.Add(crop.Value, 0);
                continue;
            }

            var weight = amount / perWeight;

            crops.Add(crop.Value, weight);
        }

        // Mushroom is a special case, it needs to be calculated dynamically based on the
        // ratio between the farmed crops that give two mushrooms per break with cow pet
        // and the farmed crops that give one mushroom per break with cow pet
        if (!collections.TryGetValue(Crop.Mushroom, out var mushroomAmount)) return crops;

        var mushroomPerWeight = collectionPerWeight["MUSHROOM_COLLECTION"];

        var totalWeight = crops.Sum(x => x.Value);
        var doubleBreakCrops = crops[Crop.Cactus] + crops[Crop.SugarCane];

        var doubleBreakRatio = doubleBreakCrops / totalWeight;
        var normalCropRatio = (totalWeight - doubleBreakCrops) / totalWeight;

        var mushroomWeight = doubleBreakRatio * (mushroomAmount / (mushroomPerWeight * 2)) +
                             normalCropRatio * (mushroomAmount / mushroomPerWeight);

        crops[Crop.Mushroom] = mushroomWeight;

        return crops;
    }

    private static Dictionary<string, double> ParseBonusWeight(ProfileMember member, IReadOnlyDictionary<string, int> craftedMinions)
    {
        var config = FarmingWeightConfig.Settings;
        var bonus = new Dictionary<string, double>();

        // Farming Skill
        var farmingXp = member.Skills.Farming;
        if (farmingXp > 111672425 && member.JacobData.Perks.LevelCap == 10)
        {
            bonus.Add("Farming Level 60", config.Farming60Bonus);
        }
        else if (farmingXp > 55172425)
        {
            bonus.Add("Farming Level 50", config.Farming50Bonus);
        }

        // Anita bonus
        if (member.JacobData.Perks.DoubleDrops > 0)
        {
            bonus.Add("Anita", member.JacobData.Perks.DoubleDrops * config.AnitaBuffBonusMultiplier);
        }

        // Contest medals
        var maxMedals = FarmingWeightConfig.Settings.MaxMedalsCounted;
        if (member.JacobData.EarnedMedals.Diamond >= maxMedals)
        {
            bonus.Add("Contest Medals", (int) (config.WeightPerDiamondMedal * config.MaxMedalsCounted));
        }
        else {
            var diamond = member.JacobData.EarnedMedals.Diamond;
            var platinum = Math.Min(maxMedals - diamond, member.JacobData.EarnedMedals.Platinum);
            var gold = Math.Min(maxMedals - diamond - platinum, member.JacobData.EarnedMedals.Gold);

            var medals = diamond * config.WeightPerDiamondMedal
             + platinum * config.WeightPerPlatinumMedal
             + gold * config.WeightPerGoldMedal;

            bonus.Add("Contest Medals", medals);
        }

        // Tier 12 farming minions
        var obtained = 0;
        foreach (var minion in config.FarmingMinions)
        {
            craftedMinions.TryGetValue(minion, out var tierBitField);
            obtained += ((tierBitField >> config.MinionRewardTier) & 1) == 1 ? 1 : 0;
        }

        if (obtained > 0)
        {
            bonus.Add("Farming Minions", obtained * config.MinionRewardWeight);
        }

        return bonus;
    }
    
    public static double CountCropWeight(this CropCollection crops) {
        var weights = FarmingWeightConfig.Settings.CropWeights;
        var result = 0.0;

        try {
            // A lot of repeated code, but it's the simplest way to do it and avoids unnecessary allocations
            result += Math.Max(crops.Carrot - PestParser.CalcUncountedCrops(Pest.Cricket, crops.Cricket), 0) / weights[Crop.Carrot];
            result += Math.Max(crops.CocoaBeans - PestParser.CalcUncountedCrops(Pest.Moth, crops.Moth), 0) / weights[Crop.CocoaBeans];
            result += Math.Max(crops.Melon - PestParser.CalcUncountedCrops(Pest.Earthworm, crops.Earthworm), 0) / weights[Crop.Melon];
            result += Math.Max(crops.NetherWart - PestParser.CalcUncountedCrops(Pest.Beetle, crops.Beetle), 0) / weights[Crop.NetherWart];
            result += Math.Max(crops.Potato - PestParser.CalcUncountedCrops(Pest.Locust, crops.Locust), 0) / weights[Crop.Potato];
            result += Math.Max(crops.Pumpkin - PestParser.CalcUncountedCrops(Pest.Rat, crops.Rat), 0) / weights[Crop.Pumpkin];
            result += Math.Max(crops.Wheat - PestParser.CalcUncountedCrops(Pest.Fly, crops.Fly), 0) / weights[Crop.Wheat];
        
            // Assign weights for mushroom and the double-break crops for special mushroom calculation
            var mushroom = Math.Max(crops.Mushroom - PestParser.CalcUncountedCrops(Pest.Slug, crops.Slug), 0) / weights[Crop.Mushroom];
            var doubleBreak = Math.Max(crops.Cactus - PestParser.CalcUncountedCrops(Pest.Mite, crops.Mite), 0) / weights[Crop.Cactus];
            doubleBreak += Math.Max(crops.SugarCane - PestParser.CalcUncountedCrops(Pest.Mosquito, crops.Mosquito), 0) / weights[Crop.SugarCane];
            result += mushroom + doubleBreak;

            var mushroomPerWeight = weights[Crop.Mushroom];
            var doubleBreakRatio = doubleBreak / result;
            var normalCropRatio = (result - doubleBreak) / result;

            var mushroomWeight = doubleBreakRatio * (crops.Mushroom / (mushroomPerWeight * 2)) +
                                 normalCropRatio * (crops.Mushroom / mushroomPerWeight);

            result -= mushroom;
            result += mushroomWeight;
        } catch (Exception e) {
            Console.Error.WriteLine(e);
            return 0;
        }
        
        return result;
    }
}
