using EliteAPI.Configuration.Settings;
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
			.Be((int) Math.Ceiling(FarmingItemsConfig.Settings.PestCropDropChances[Pest.Mite].GetCropsToSubtract(250) * 1));

		PestParser.CalcUncountedCrops(Pest.Cricket, 426).Should().Be(834343);
	}
	
	[Fact]
	public void PestCollectionsTest() {
		FarmingItemsConfig.Settings.PestCropDropChances[Pest.Slug].GetCropsToSubtract(1300, true, false)
			.Should().BeApproximately(2402.13, 0.01);
		
		FarmingItemsConfig.Settings.PestCropDropChances[Pest.Slug].GetCropsToSubtract(0, true, false)
			.Should().BeApproximately(211.2, 0.01);
		
		FarmingItemsConfig.Settings.PestCropDropChances[Pest.Fly].GetCropsToSubtract(1300, true, false)
			.Should().BeApproximately(24053.76, 0.01);
		
		FarmingItemsConfig.Settings.PestCropDropChances[Pest.Fly].GetCropsToSubtract(0, true, false)
			.Should().BeApproximately(3162.24, 0.01);
	}
	
	
	public PestParserTests() {
		FarmingItemsConfig.Settings.PestDropBrackets = new Dictionary<string, int> {
			{ "0", 0 }, { "50", 250 }, { "100", 500 }, { "250", 750 },
			{ "500", 1000 }, { "750", 1250 }, { "1000", 1500 }
		};

		FarmingItemsConfig.Settings.PestCropDropChances = new Dictionary<Pest, PestDropChance> {
			{
				Pest.Mite, new PestDropChance {
					Base = 160,
					Rare = [
						new PestRngDrop {
							Drops = 25600,
							Chance = 0.02
						}
					]
				}
			}, {
				Pest.Cricket, new PestDropChance {
					Base = 160,
					Rare = [
						new PestRngDrop {
							Drops = 20480,
							Chance = 0.03
						}
					]
				}
			}, {
				Pest.Moth, new PestDropChance {
					Base = 160,
					Rare = [
						new PestRngDrop {
							Drops = 20480,
							Chance = 0.03
						}
					]
				}
			}, {
				Pest.Earthworm, new PestDropChance {
					Base = 160,
					Rare = [
						new PestRngDrop {
							Drops = 25600,
							Chance = 0.04
						}
					]
				}
			}, {
				Pest.Slug, new PestDropChance {
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
				Pest.Beetle, new PestDropChance {
					Base = 160,
					Rare = [
						new PestRngDrop {
							Drops = 25600,
							Chance = 0.03
						}
					]
				}
			}, {
				Pest.Locust, new PestDropChance {
					Base = 160,
					Rare = [
						new PestRngDrop {
							Drops = 25600,
							Chance = 0.03
						}
					]
				}
			}, {
				Pest.Rat, new PestDropChance {
					Base = 160,
					Rare = [
						new PestRngDrop {
							Drops = 25600,
							Chance = 0.01
						}
					]
				}
			}, {
				Pest.Mosquito, new PestDropChance {
					Base = 160,
					Rare = [
						new PestRngDrop {
							Drops = 25600,
							Chance = 0.02
						}
					]
				}
			}, {
				Pest.Fly, new PestDropChance {
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