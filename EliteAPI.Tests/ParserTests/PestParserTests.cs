using EliteAPI.Configuration.Settings;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Parsers.Farming;
using EliteAPI.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EliteAPI.Tests.ParserTests;

public class PestParserTests {
	[Fact]
	public void ParsePestCropCollectionNumbersTest() {
		var configurationBuilder = new ConfigurationBuilder();
		configurationBuilder.RegisterEliteConfigFiles();
		var configuration = configurationBuilder.Build();
		
		var services = new ServiceCollection();
		services.Configure<ConfigFarmingWeightSettings>(configuration.GetSection("FarmingWeight"));
		var serviceProvider = services.BuildServiceProvider();
		
		var weightConfig = serviceProvider.GetRequiredService<IOptions<ConfigFarmingWeightSettings>>().Value;

		PestParser.CalcUncountedCrops(Pest.Mite, 0, weightConfig).Should().Be(0);
		PestParser.CalcUncountedCrops(Pest.Mite, 25, weightConfig).Should().Be(0);
		PestParser.CalcUncountedCrops(Pest.Mite, 50, weightConfig).Should().Be(0);
		PestParser.CalcUncountedCrops(Pest.Mite, 51, weightConfig).Should()
			.Be((int) Math.Ceiling(weightConfig.PestCropDropChances[Pest.Mite].GetCropsToSubtract(250, weightConfig: weightConfig) * 1));

		PestParser.CalcUncountedCrops(Pest.Cricket, 426, weightConfig).Should().Be(370802);
	}
	
	[Fact]
	public void PestCollectionsTest() {
		var configurationBuilder = new ConfigurationBuilder();
		configurationBuilder.RegisterEliteConfigFiles();
		var configuration = configurationBuilder.Build();
		
		var services = new ServiceCollection();
		services.Configure<ConfigFarmingWeightSettings>(configuration.GetSection("FarmingWeight"));
		var serviceProvider = services.BuildServiceProvider();
		
		var weightConfig = serviceProvider.GetRequiredService<IOptions<ConfigFarmingWeightSettings>>().Value;
		
		weightConfig.PestCropDropChances[Pest.Slug].GetCropsToSubtract(1300, true, false, weightConfig)
			.Should().BeApproximately(662.77, 0.01);
		
		weightConfig.PestCropDropChances[Pest.Slug].GetCropsToSubtract(0, true, false, weightConfig)
			.Should().BeApproximately(49.26, 0.01);
		
		weightConfig.PestCropDropChances[Pest.Fly].GetCropsToSubtract(1300, true, false, weightConfig)
			.Should().BeApproximately(0.0, 0.01);
		
		weightConfig.PestCropDropChances[Pest.Fly].GetCropsToSubtract(0, true, false, weightConfig)
			.Should().BeApproximately(0.0, 0.01);
	}
}