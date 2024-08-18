using EliteAPI.Configuration.Settings;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Models.Entities.Timescale;
using EliteAPI.Parsers.Farming;

namespace EliteAPI.Tests.WeightTests;

public class WeightParserTests {
	
	[Fact]
	public void CropWeightTests() {
		var collection = new CropCollection() {
			ProfileMemberId = new Guid(),

			Cactus = 6907586,
			Carrot = 10134865,
			CocoaBeans = 6100018,
			Melon = 4257987327,
			Mushroom = 369064913,
			NetherWart = 8150463,
			Potato = 10033780,
			Pumpkin = 7838879,
			SugarCane = 48765077,
			Wheat = 9189432517,
			Seeds = 8122998326,
			
			Mite = 768,
			Cricket = 828,
			Moth = 765,
			Earthworm = 712,
			Slug = 780,
			Beetle = 763,
			Locust = 839,
			Rat = 772,
			Mosquito = 778,
			Fly = 5696
		};

		var collections = new Dictionary<string, long>() {
			{ CropId.Cactus, collection.Cactus },
			{ CropId.Carrot, collection.Carrot },
			{ CropId.CocoaBeans, collection.CocoaBeans },
			{ CropId.Melon, collection.Melon },
			{ CropId.Mushroom, collection.Mushroom },
			{ CropId.NetherWart, collection.NetherWart },
			{ CropId.Potato, collection.Potato },
			{ CropId.Pumpkin, collection.Pumpkin },
			{ CropId.SugarCane, collection.SugarCane },
			{ CropId.Wheat, collection.Wheat }
		};
		var uncounted = collection.CalcUncountedCrops();
		
		var weight = FarmingWeightParser.ParseCropWeight(collections, uncounted);
		var summed = weight.Values.Sum();

		collection.CountCropWeight().Should().BeApproximately(summed, 0.001);
	}
	
	
	public WeightParserTests() {
		FarmingWeightConfig.Settings.CropsPerOneWeight = new() {
			{ "CACTUS", 177254.45 },
			{ "CARROT_ITEM", 302061.86 },
			{ "INK_SACK_3", 267174.04 },
			{ "MELON", 485308.47 },
			{ "MUSHROOM_COLLECTION", 90178.06 },
			{ "NETHER_STALK", 250000 },
			{ "POTATO_ITEM", 300000 },
			{ "PUMPKIN", 98284.71 },
			{ "SUGAR_CANE", 200000 },
			{ "WHEAT", 100000 }
		};
		
		FarmingWeightConfig.Settings.CropItemIds = FarmingWeightConfig.Settings.CropsPerOneWeight.Keys
			.Select(c => c == "INK_SACK_3" ? "INK_SACK:3" : c).ToList();
		
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