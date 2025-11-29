using HypixelAPI.Networth.Calculators.Handlers;
using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;
using HypixelAPI.Networth.Tests.Infrastructure;

namespace HypixelAPI.Networth.Tests.Handlers;

public class JalapenoBookHandlerTests : BaseHandlerTest<JalapenoBookHandler>
{
	[Fact]
	public void RunTestCases() {
		var testCases = new List<HandlerTestCase> {
			new() {
				Description = "Applies correctly",
				Item = new {
					SkyblockId = "SOS_FLARE",
					Attributes = new { Extra = new { jalapeno_count = 4 } },
					Price = 100
				},
				Prices = new Dictionary<string, double> { { "JALAPENO_BOOK", 31000000 } },
				ShouldApply = true,
				ExpectedPriceChange = 4 * 31000000 * NetworthConstants.ApplicationWorth.JalapenoBook,
				ExpectedCalculation = new List<NetworthCalculation> {
					new() {
						Id = "JALAPENO_BOOK",
						Type = "JALAPENO_BOOK",
						Value = 4 * 31000000 * NetworthConstants.ApplicationWorth.JalapenoBook,
						Count = 4
					}
				}
			},
			new() {
				Description = "Does not apply",
				Item = new {
					SkyblockId = "SOS_FLARE",
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