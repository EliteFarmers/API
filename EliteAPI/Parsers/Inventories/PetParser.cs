using EliteAPI.Configuration.Settings;
using EliteAPI.Models.DTOs.Outgoing;
using HypixelAPI.DTOs;

namespace EliteAPI.Parsers.Inventories;

public static class PetParser
{
    public static int GetLevel(this ItemPetInfoDto pet)
    {
	    return GetPetLevel(pet.Type, pet.Tier, pet.Exp);
    }

    public static int GetLevel(this PetDto pet)
    {
	    return GetPetLevel(pet.Type, pet.Tier ?? "COMMON", (decimal) pet.Exp);
    }
    
    public static int GetLevel(this PetResponse pet)
    {
	    if (pet.Type is null) return 0;
	    return GetPetLevel(pet.Type, pet.Tier ?? "COMMON", (decimal) pet.Exp);
    }
    
    private static int GetPetLevel(string type, string tier, decimal xp)
	{
		var config = SkyblockPetConfig.Settings;
	    
		var offset = config.RarityOffsets.TryGetValue(tier, out var value) ? value : 0;
		var maxLevel = config.MaxLevels.TryGetValue(type, out var max) ? max : 100;

		for (var i = offset; i < Math.Min(config.Levels.Count, maxLevel + offset); i++)
		{
			if (i >= config.Levels.Count) break;
			var level = config.Levels[i];

			if (xp < level)
			{
				return i + 1 - offset;
			}

			xp -= level;
		}
	    
		return maxLevel;
	}
}