using System.Configuration;
using System.Text.Json;
using EliteAPI.Config.Settings;
using EliteAPI.Models.DTOs.Incoming;
using EliteAPI.Models.Entities.Farming;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Parsers.Inventories;
using EliteAPI.Utilities;

namespace EliteAPI.Parsers.FarmingWeight;

public static class FarmingWeightParser
{
    public static async Task ParseFarmingWeight(this ProfileMember member, Dictionary<string, int> craftedMinions, RawMemberData memberData)
    {
        member.Farming.ProfileMemberId = member.Id;
        member.Farming.ProfileMember = member;

        member.Farming.CropWeight = ParseCropWeight(member.Collections);
        member.Farming.BonusWeight = ParseBonusWeight(member, craftedMinions);

        member.Farming.TotalWeight = member.Farming.CropWeight.Sum(x => x.Value) 
                                           + member.Farming.BonusWeight.Sum(x => x.Value);

        member.Farming.Inventory = await memberData.ExtractFarmingItems(member);
    }

    public static Dictionary<string, double> ParseCropWeight(JsonDocument jsonCollections) {
        var collections = jsonCollections.Deserialize<Dictionary<string, long>>() ?? new Dictionary<string, long>();
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

        crops["Mushroom"] = mushroomWeight;

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

        // Gold medals
        if (member.JacobData.EarnedMedals.Gold > FarmingWeightConfig.Settings.MaxMedalsCounted)
        {
            bonus.Add("Gold Medals", (int) (config.WeightPerGoldMedal * config.MaxMedalsCounted));
        }
        else
        {
            var rewardCount = (member.JacobData.EarnedMedals.Gold / 50) * 50;
            if (rewardCount > 0)
            {
                bonus.Add("Gold Medals", (int) (config.WeightPerGoldMedal * rewardCount));
            }
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
}
