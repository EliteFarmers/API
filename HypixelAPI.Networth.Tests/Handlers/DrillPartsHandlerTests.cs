using HypixelAPI.Networth.Calculators.Handlers;
using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;
using HypixelAPI.Networth.Tests.Infrastructure;

namespace HypixelAPI.Networth.Tests.Handlers;

public class DrillPartsHandlerTests : BaseHandlerTest<DrillPartsHandler>
{
	[Fact]
	public void RunTestCases() {
		var testCases = new List<HandlerTestCase> {
			new() {
				Description = "Applies correctly",
				Item = new {
					SkyblockId = "TITANIUM_DRILL_1",
					Attributes = new {
						Extra = new {
							drill_part_engine = "amber_polished_drill_engine",
							drill_part_fuel_tank = "perfectly_cut_fuel_tank"
						}
					},
					Price = 100
				},
				Prices = new Dictionary<string, double>
					{ { "AMBER_POLISHED_DRILL_ENGINE", 250000000 }, { "PERFECTLY_CUT_FUEL_TANK", 100000000 } },
				ShouldApply = true,
				ExpectedPriceChange = 250000000 * NetworthConstants.ApplicationWorth.DrillPart +
				                      100000000 * NetworthConstants.ApplicationWorth.DrillPart,
				ExpectedCalculation = new List<NetworthCalculation> {
					new() {
						Id = "PERFECTLY_CUT_FUEL_TANK",
						Type = "DRILL_PART",
						Value = 100000000 * NetworthConstants.ApplicationWorth.DrillPart,
						Count = 1
					},
					new() {
						Id = "AMBER_POLISHED_DRILL_ENGINE",
						Type = "DRILL_PART",
						Value = 250000000 * NetworthConstants.ApplicationWorth.DrillPart,
						Count = 1
					}
				}
			},
			new() {
				Description = "Does not apply",
				Item = new {
					SkyblockId = "TITANIUM_DRILL_1",
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