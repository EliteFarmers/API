using EliteAPI.Models.Entities.Hypixel;
using EliteFarmers.HypixelAPI.DTOs;

namespace EliteAPI.Parsers.Farming;

public static class UnlockedPlotsParser
{
	private static readonly Dictionary<string, UnlockedPlots> PlotsMap = new() {
		{ "beginner_1", UnlockedPlots.Beginner1 },
		{ "beginner_2", UnlockedPlots.Beginner2 },
		{ "beginner_3", UnlockedPlots.Beginner3 },
		{ "beginner_4", UnlockedPlots.Beginner4 },
		{ "intermediate_1", UnlockedPlots.Intermediate1 },
		{ "intermediate_2", UnlockedPlots.Intermediate2 },
		{ "intermediate_3", UnlockedPlots.Intermediate3 },
		{ "intermediate_4", UnlockedPlots.Intermediate4 },
		{ "advanced_1", UnlockedPlots.Advanced1 },
		{ "advanced_2", UnlockedPlots.Advanced2 },
		{ "advanced_3", UnlockedPlots.Advanced3 },
		{ "advanced_4", UnlockedPlots.Advanced4 },
		{ "advanced_5", UnlockedPlots.Advanced5 },
		{ "advanced_6", UnlockedPlots.Advanced6 },
		{ "advanced_7", UnlockedPlots.Advanced7 },
		{ "advanced_8", UnlockedPlots.Advanced8 },
		{ "advanced_9", UnlockedPlots.Advanced9 },
		{ "advanced_10", UnlockedPlots.Advanced10 },
		{ "advanced_11", UnlockedPlots.Advanced11 },
		{ "advanced_12", UnlockedPlots.Advanced12 },
		{ "expert_1", UnlockedPlots.Expert1 },
		{ "expert_2", UnlockedPlots.Expert2 },
		{ "expert_3", UnlockedPlots.Expert3 },
		{ "expert_4", UnlockedPlots.Expert4 }
	};

	public static uint CombinePlots(List<string> plots) {
		var combined = UnlockedPlots.None;

		foreach (var plot in plots) {
			if (!PlotsMap.TryGetValue(plot, out var value)) continue;
			combined |= value;
		}

		return (uint)combined;
	}

	public static uint CombinePlots(this GardenResponseData garden) {
		return CombinePlots(garden.UnlockedPlots);
	}

	public static List<string> SeperatePlots(uint plots) {
		var seperated = new List<string>();

		foreach (var (key, value) in PlotsMap) {
			if ((plots & (uint)value) != 0) seperated.Add(key);
		}

		return seperated;
	}

	public static List<string> SeparatePlots(this UnlockedPlots plots) {
		return SeperatePlots((uint)plots);
	}
}