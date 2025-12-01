using HypixelAPI.Networth.Calculators.Handlers;
using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;
using HypixelAPI.Networth.Tests.Infrastructure;

namespace HypixelAPI.Networth.Tests.Handlers;

public class NecronBladeScrollsHandlerTests : BaseHandlerTest<NecronBladeScrollsHandler>
{
	[Fact]
	public void RunTestCases() {
		var testCases = new List<HandlerTestCase> {
			new() {
				Description = "Applies correctly",
				Item = new {
					SkyblockId = "HYPERION",
					Attributes = new
						{ Extra = new { ability_scroll = new[] { "WITHER_SHIELD_SCROLL", "IMPLOSION_SCROLL" } } },
					Price = 100
				},
				Prices = new Dictionary<string, double>
					{ { "WITHER_SHIELD_SCROLL", 280000000 }, { "IMPLOSION_SCROLL", 300000000 } },
				ShouldApply = true,
				ExpectedPriceChange = 280000000 * NetworthConstants.ApplicationWorth.NecronBladeScroll +
				                      300000000 * NetworthConstants.ApplicationWorth.NecronBladeScroll,
				ExpectedCalculation = new List<NetworthCalculation> {
					new() {
						Id = "WITHER_SHIELD_SCROLL",
						Type = "NECRON_SCROLL",
						Value = 280000000 * NetworthConstants.ApplicationWorth.NecronBladeScroll,
						Count = 1
					},
					new() {
						Id = "IMPLOSION_SCROLL",
						Type = "NECRON_SCROLL",
						Value = 300000000 * NetworthConstants.ApplicationWorth.NecronBladeScroll,
						Count = 1
					}
				}
			},
			new() {
				Description = "Does not apply",
				Item = new {
					SkyblockId = "HYPERION",
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