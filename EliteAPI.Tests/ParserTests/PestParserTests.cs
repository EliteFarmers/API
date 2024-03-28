using EliteAPI.Config.Settings;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Parsers.Farming;

namespace EliteAPI.Tests.ParserTests;

public class PestParserTests {
	
	[Fact]
	public void ParsePestCropCollectionNumbersTest() {
		PestParser.CalcUncountedCrops(Pest.Mite, 0).Should().Be(0);
		PestParser.CalcUncountedCrops(Pest.Mite, 25).Should().Be(0);
		PestParser.CalcUncountedCrops(Pest.Mite, 50).Should().Be(0);
		PestParser.CalcUncountedCrops(Pest.Mite, 51).Should()
			.Be((int) Math.Ceiling(FarmingItemsConfig.Settings.PestCropDropChances[Pest.Mite].GetChance(250) * 1));

		PestParser.CalcUncountedCrops(Pest.Cricket, 426).Should().Be(834343);
	}
	
	
	public PestParserTests() {
		FarmingItemsConfig.Settings.PestDropBrackets = new Dictionary<string, int> {
			{ "0", 0 }, { "50", 250 }, { "100", 500 }, { "250", 750 },
			{ "500", 1000 }, { "750", 1250 }, { "1000", 1500 }
		};

		FarmingItemsConfig.Settings.PestCropDropChances = new Dictionary<Pest, PestDropChance> {
			{
				Pest.Mite, new() {
					Base = 160,
					Rare = [
						new PestRngDrop {
							Drops = 25600,
							Chance = 0.02
						}
					]
				}
			}, {
				Pest.Cricket, new() {
					Base = 160,
					Rare = [
						new PestRngDrop {
							Drops = 20480,
							Chance = 0.03
						}
					]
				}
			}, {
				Pest.Moth, new() {
					Base = 160,
					Rare = [
						new PestRngDrop {
							Drops = 20480,
							Chance = 0.03
						}
					]
				}
			}, {
				Pest.Earthworm, new() {
					Base = 160,
					Rare = [
						new PestRngDrop {
							Drops = 25600,
							Chance = 0.04
						}
					]
				}
			}, {
				Pest.Slug, new() {
					Base = 160,
					Rare = [
						new PestRngDrop {
							Drops = 5120,
							Chance = 0.005
						},
						new PestRngDrop {
							Drops = 5120,
							Chance = 0.005
						}
					]
				}
			}, {
				Pest.Beetle, new() {
					Base = 160,
					Rare = [
						new PestRngDrop {
							Drops = 25600,
							Chance = 0.03
						}
					]
				}
			}, {
				Pest.Locust, new() {
					Base = 160,
					Rare = [
						new PestRngDrop {
							Drops = 25600,
							Chance = 0.03
						}
					]
				}
			}, {
				Pest.Rat, new() {
					Base = 160,
					Rare = [
						new PestRngDrop {
							Drops = 25600,
							Chance = 0.01
						}
					]
				}
			}, {
				Pest.Mosquito, new() {
					Base = 160,
					Rare = [
						new PestRngDrop {
							Drops = 25600,
							Chance = 0.02
						}
					]
				}
			}, {
				Pest.Fly, new() {
					Base = 1296,
					Rare = [
						new PestRngDrop {
							Drops = 186624,
							Chance = 0.01
						}
					]
				}
			}
		};
	}
}