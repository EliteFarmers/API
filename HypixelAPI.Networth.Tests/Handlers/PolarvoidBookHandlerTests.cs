using HypixelAPI.Networth.Calculators.Handlers;
using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;
using HypixelAPI.Networth.Tests.Infrastructure;

namespace HypixelAPI.Networth.Tests.Handlers;

public class PolarvoidBookHandlerTests : BaseHandlerTest<PolarvoidBookHandler>
{
	[Fact]
	public void RunTestCases() {
		var testCases = new List<HandlerTestCase> {
			new() {
				Description = "Applies correctly",
				Item = new {
					SkyblockId = "TITANIUM_DRILL_2",
					Attributes = new { Extra = new { polarvoid = 5 } },
					Price = 100
				},
				Prices = new Dictionary<string, double> { { "POLARVOID_BOOK", 2500000 } },
				ShouldApply = true,
				ExpectedPriceChange = 5 * 2500000 * NetworthConstants.ApplicationWorth.PolarvoidBook,
				ExpectedCalculation = new List<NetworthCalculation> {
					new() {
						Id = "POLARVOID_BOOK",
						Type = "POLARVOID_BOOK",
						Value = 5 * 2500000 * NetworthConstants.ApplicationWorth.PolarvoidBook,
						Count = 5
					}
				}
			},
			new() {
				Description = "Does not apply",
				Item = new {
					SkyblockId = "TITANIUM_DRILL_2",
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