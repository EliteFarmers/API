using HypixelAPI.Networth.Calculators.Handlers;
using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;
using HypixelAPI.Networth.Tests.Infrastructure;

namespace HypixelAPI.Networth.Tests.Handlers;

public class EtherwarpConduitHandlerTests : BaseHandlerTest<EtherwarpConduitHandler>
{
	[Fact]
	public void RunTestCases() {
		var testCases = new List<HandlerTestCase> {
			new() {
				Description = "Applies correctly",
				Item = new {
					SkyblockId = "ASPECT_OF_THE_VOID",
					Attributes = new { Extra = new { ethermerge = "1b" } },
					Price = 100
				},
				Prices = new Dictionary<string, double> { { "ETHERWARP_CONDUIT", 15000000 } },
				ShouldApply = true,
				ExpectedPriceChange = 15000000 * NetworthConstants.ApplicationWorth.Etherwarp,
				ExpectedCalculation = new List<NetworthCalculation> {
					new() {
						Id = "ETHERWARP_CONDUIT",
						Type = "ETHERWARP_CONDUIT",
						Value = 15000000 * NetworthConstants.ApplicationWorth.Etherwarp,
						Count = 1
					}
				}
			},
			new() {
				Description = "Does not apply",
				Item = new {
					SkyblockId = "ASPECT_OF_THE_END",
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