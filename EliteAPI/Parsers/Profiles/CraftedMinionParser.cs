using EliteAPI.Models.Entities.Hypixel;

namespace EliteAPI.Parsers.Profiles;

public static class CraftedMinionParser
{
	public static void CombineMinions(this Profile profile, string[]? minionStrings) {
		profile.CraftedMinions = CombineMinions(profile.CraftedMinions, minionStrings);
	}

	public static Dictionary<string, int> CombineMinions(Dictionary<string, int> craftedMinions,
		string[]? minionStrings) {
		if (minionStrings is null) return craftedMinions;

		// Ex: "WHEAT_1", "SUGAR_CANE_1"
		foreach (var minion in minionStrings) {
			// Split at last underscore of multiple underscores
			var lastUnderscore = minion.LastIndexOf("_", StringComparison.Ordinal);

			var minionType = minion[..lastUnderscore];
			var minionLevel = minion[(lastUnderscore + 1)..];

			if (!int.TryParse(minionLevel, out var level)) continue;

			craftedMinions.TryGetValue(minionType, out var current);
			// Set the bit at the level to 1
			craftedMinions[minionType] = current | (1 << level);
		}

		return craftedMinions;
	}
}