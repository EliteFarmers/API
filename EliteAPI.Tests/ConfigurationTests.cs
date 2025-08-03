using EliteAPI.Utilities;
using Microsoft.Extensions.Configuration;

namespace EliteAPI.Tests;

public class ConfigurationTests
{
	private readonly IConfiguration _configuration;

	public ConfigurationTests()
	{
		var configurationBuilder = new ConfigurationBuilder();
		configurationBuilder.RegisterEliteConfigFiles();
		_configuration = configurationBuilder.Build();
	}

	[Fact]
	public void TestConfigurationContainsExpectedSection()
	{
		var farmingWeightSection = _configuration.GetSection("FarmingWeight");
		farmingWeightSection.ShouldNotBeNull();
	}
}