using HypixelAPI.Networth.Calculators.Handlers;
using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;
using HypixelAPI.Networth.Tests.Infrastructure;
using Xunit;

namespace HypixelAPI.Networth.Tests.Handlers;

public class BoosterHandlerTests : BaseHandlerTest<BoosterHandler>
{
	[Fact]
	public void RunTestCases() {
		var testCases = new List<HandlerTestCase> {
			new() {
				Description = "Applies correctly",
				Item = new {
					SkyblockId = "FIGSTONE_AXE",
					Attributes = new { Extra = new { boosters = new[] { "sweep" } } },
					Price = 100
				},
				Prices = new Dictionary<string, double> { { "SWEEP_BOOSTER", 100000 } },
				ShouldApply = true,
				ExpectedPriceChange = 100000 * NetworthConstants.ApplicationWorth.Booster,
				ExpectedCalculation = new List<NetworthCalculation> {
					new() {
						Id = "SWEEP_BOOSTER",
						Type = "BOOSTER",
						Value = 100000 * NetworthConstants.ApplicationWorth.Booster,
						Count = 1
					}
				}
			},
			new() {
				Description = "Does not apply",
				Item = new {
					SkyblockId = "FIGSTONE_AXE",
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