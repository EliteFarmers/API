using HypixelAPI.Networth.Calculators.Handlers;
using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;
using HypixelAPI.Networth.Tests.Infrastructure;

namespace HypixelAPI.Networth.Tests.Handlers;

public class ManaDisintegratorHandlerTests : BaseHandlerTest<ManaDisintegratorHandler>
{
	[Fact]
	public void RunTestCases() {
		var testCases = new List<HandlerTestCase> {
			new() {
				Description = "Applies correctly",
				Item = new {
					SkyblockId = "WAND_OF_ATONEMENT",
					Attributes = new { Extra = new { mana_disintegrator_count = 10 } },
					Price = 100
				},
				Prices = new Dictionary<string, double> { { "MANA_DISINTEGRATOR", 35000 } },
				ShouldApply = true,
				ExpectedPriceChange = 10 * 35000 * NetworthConstants.ApplicationWorth.ManaDisintegrator,
				ExpectedCalculation = new List<NetworthCalculation> {
					new() {
						Id = "MANA_DISINTEGRATOR",
						Type = "MANA_DISINTEGRATOR",
						Value = 10 * 35000 * NetworthConstants.ApplicationWorth.ManaDisintegrator,
						Count = 10
					}
				}
			},
			new() {
				Description = "Does not apply",
				Item = new {
					SkyblockId = "WAND_OF_ATONEMENT",
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