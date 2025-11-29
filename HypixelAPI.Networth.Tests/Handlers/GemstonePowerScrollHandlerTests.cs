using HypixelAPI.Networth.Calculators.Handlers;
using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;
using HypixelAPI.Networth.Tests.Infrastructure;

namespace HypixelAPI.Networth.Tests.Handlers;

public class GemstonePowerScrollHandlerTests : BaseHandlerTest<GemstonePowerScrollHandler>
{
	[Fact]
	public void RunTestCases() {
		var testCases = new List<HandlerTestCase> {
			new() {
				Description = "Applies correctly",
				Item = new {
					SkyblockId = "FLORID_ZOMBIE_SWORD",
					Attributes = new { Extra = new { power_ability_scroll = "POWER_SCROLL" } },
					Price = 100
				},
				Prices = new Dictionary<string, double> { { "GEMSTONE_POWER_SCROLL", 650000 } },
				ShouldApply = true,
				ExpectedPriceChange = 650000 * NetworthConstants.ApplicationWorth.GemstonePowerScroll,
				ExpectedCalculation = new List<NetworthCalculation> {
					new() {
						Id = "GEMSTONE_POWER_SCROLL",
						Type = "GEMSTONE_POWER_SCROLL",
						Value = 650000 * NetworthConstants.ApplicationWorth.GemstonePowerScroll,
						Count = 1
					}
				}
			},
			new() {
				Description = "Does not apply",
				Item = new {
					SkyblockId = "FLORID_ZOMBIE_SWORD",
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