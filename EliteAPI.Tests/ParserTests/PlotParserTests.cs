using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Parsers.Farming;

namespace EliteAPI.Tests.ParserTests;

public class PlotParserTests {
	
	[Fact]
	public void CombinePlotsTest() {
		List<string> plots = [
			"beginner_1",
			"beginner_2",
			"beginner_3",
			"beginner_4",
			"intermediate_1",
			"intermediate_2",
			"intermediate_3",
			"intermediate_4",
			"advanced_1",
			"advanced_2",
			"advanced_3",
			"advanced_4",
			"advanced_5",
			"advanced_6",
			"advanced_7",
			"advanced_8",
			"advanced_9",
			"advanced_10",
			"advanced_11",
			"advanced_12",
			"expert_1",
			"expert_2",
			"expert_3",
			"expert_4"
		];

		var result = UnlockedPlotsParser.CombinePlots(plots);
		var unlocked = (UnlockedPlots)result;

		(unlocked == UnlockedPlots.All).Should().BeTrue();
		
		UnlockedPlotsParser.SeperatePlots(result).Should().Equal(plots);
	}
}