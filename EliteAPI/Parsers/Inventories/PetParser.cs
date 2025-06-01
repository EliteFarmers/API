using EliteAPI.Configuration.Settings;
using EliteAPI.Models.DTOs.Outgoing;

namespace EliteAPI.Parsers.Inventories;

public static class PetParser
{
    public static int GetLevel(this ItemPetInfoDto pet)
    {
	    var config = SkyblockPetConfig.Settings;
	    
	    var offset = config.RarityOffsets.TryGetValue(pet.Tier, out var value) ? value : 0;
	    var maxLevel = config.MaxLevels.TryGetValue(pet.Type, out var max) ? max : 100;

	    var xp = pet.Exp;
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