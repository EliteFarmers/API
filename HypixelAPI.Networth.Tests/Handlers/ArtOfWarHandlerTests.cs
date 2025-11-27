using HypixelAPI.Networth.Calculators.Handlers;
using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;
using HypixelAPI.Networth.Tests.Infrastructure;

namespace HypixelAPI.Networth.Tests.Handlers;

public class ArtOfWarHandlerTests : BaseHandlerTest<ArtOfWarHandler>
{
	[Fact]
	public void RunTestCases() {
		var testCases = new List<HandlerTestCase> {
			new() {
				Description = "Applies correctly",
				Item = new {
					SkyblockId = "IRON_SWORD",
					Attributes = new { Extra = new { art_of_war_count = 1 } },
					Price = 100
				},
				Prices = new Dictionary<string, double> { { "THE_ART_OF_WAR", 20000000 } },
				ShouldApply = true,
				ExpectedPriceChange = 20000000 * NetworthConstants.ApplicationWorth.ArtOfWar,
				ExpectedCalculation = new List<NetworthCalculation> {
					new() {
						Id = "THE_ART_OF_WAR",
						Type = "THE_ART_OF_WAR",
						Value = 20000000 * NetworthConstants.ApplicationWorth.ArtOfWar,
						Count = 1
					}
				}
			},
			new() {
				Description = "Does not apply",
				Item = new {
					SkyblockId = "IRON_SWORD",
					Attributes = new { Extra = new { } },
					Price = 100
				},
				Prices = new Dictionary<string, double>(),
				ShouldApply = false
			}
		};

		RunTests(testCases);
	}
}