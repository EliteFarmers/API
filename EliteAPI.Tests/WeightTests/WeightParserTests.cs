using EliteAPI.Configuration.Settings;
using EliteAPI.Models.Entities.Timescale;
using EliteAPI.Parsers.Farming;
using EliteAPI.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EliteAPI.Tests.WeightTests;

public class WeightParserTests {
	[Fact]
	public void CropWeightTests() {
		var collection = new CropCollection {
			ProfileMemberId = Guid.Empty,

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

		var collections = new Dictionary<string, long> {
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

		collection.CountCropWeight().ShouldBe(summed, 0.001);
	}


	public WeightParserTests() {
		var configurationBuilder = new ConfigurationBuilder();
		configurationBuilder.RegisterEliteConfigFiles();
		var configuration = configurationBuilder.Build();

		var services = new ServiceCollection();
		services.Configure<ConfigFarmingWeightSettings>(configuration.GetSection("FarmingWeight"));
		var serviceProvider = services.BuildServiceProvider();

		FarmingWeightConfig.Settings =
			serviceProvider.GetRequiredService<IOptions<ConfigFarmingWeightSettings>>().Value;
	}
}