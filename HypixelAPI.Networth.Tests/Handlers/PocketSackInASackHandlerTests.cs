using HypixelAPI.Networth.Calculators.Handlers;
using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;
using HypixelAPI.Networth.Tests.Infrastructure;

namespace HypixelAPI.Networth.Tests.Handlers;

public class PocketSackInASackHandlerTests : BaseHandlerTest<PocketSackInASackHandler>
{
	[Fact]
	public void RunTestCases() {
		var testCases = new List<HandlerTestCase> {
			new() {
				Description = "Applies correctly",
				Item = new {
					SkyblockId = "LARGE_HUSBANDRY_SACK",
					Attributes = new { Extra = new { sack_pss = 3 } },
					Price = 100
				},
				Prices = new Dictionary<string, double> { { "POCKET_SACK_IN_A_SACK", 12000000 } },
				ShouldApply = true,
				ExpectedPriceChange = 3 * 12000000 * NetworthConstants.ApplicationWorth.PocketSackInASack,
				ExpectedCalculation = new List<NetworthCalculation> {
					new() {
						Id = "POCKET_SACK_IN_A_SACK",
						Type = "POCKET_SACK_IN_A_SACK",
						Value = 3 * 12000000 * NetworthConstants.ApplicationWorth.PocketSackInASack,
						Count = 3
					}
				}
			},
			new() {
				Description = "Does not apply",
				Item = new {
					SkyblockId = "LARGE_HUSBANDRY_SACK",
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