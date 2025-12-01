using HypixelAPI.Networth.Calculators.Handlers;
using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;
using HypixelAPI.Networth.Tests.Infrastructure;

namespace HypixelAPI.Networth.Tests.Handlers;

public class PulseRingThunderHandlerTests : BaseHandlerTest<PulseRingThunderHandler>
{
	[Fact]
	public void RunTestCases() {
		var testCases = new List<HandlerTestCase> {
			new() {
				Description = "Applies correctly",
				Item = new {
					SkyblockId = "PULSE_RING",
					Attributes = new { Extra = new { thunder_charge = 100000 } },
					Price = 100
				},
				Prices = new Dictionary<string, double> { { "THUNDER_IN_A_BOTTLE", 3000000 } },
				ShouldApply = true,
				ExpectedPriceChange = 2 * 3000000 * NetworthConstants.ApplicationWorth.ThunderInABottle,
				ExpectedCalculation = new List<NetworthCalculation> {
					new() {
						Id = "THUNDER_IN_A_BOTTLE",
						Type = "THUNDER_CHARGE",
						Value = 2 * 3000000 * NetworthConstants.ApplicationWorth.ThunderInABottle,
						Count = 2
					}
				}
			},
			new() {
				Description = "Applies correctly when above max",
				Item = new {
					SkyblockId = "PULSE_RING",
					Attributes = new { Extra = new { thunder_charge = 5050000 } },
					Price = 100
				},
				Prices = new Dictionary<string, double> { { "THUNDER_IN_A_BOTTLE", 3000000 } },
				ShouldApply = true,
				ExpectedPriceChange = 100 * 3000000 * NetworthConstants.ApplicationWorth.ThunderInABottle,
				ExpectedCalculation = new List<NetworthCalculation> {
					new() {
						Id = "THUNDER_IN_A_BOTTLE",
						Type = "THUNDER_CHARGE",
						Value = 100 * 3000000 * NetworthConstants.ApplicationWorth.ThunderInABottle,
						Count = 100
					}
				}
			},
			new() {
				Description = "Does not apply",
				Item = new {
					SkyblockId = "PULSE_RING",
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