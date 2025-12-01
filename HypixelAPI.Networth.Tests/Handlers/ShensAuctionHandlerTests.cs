using HypixelAPI.Networth.Calculators.Handlers;
using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;
using HypixelAPI.Networth.Tests.Infrastructure;

namespace HypixelAPI.Networth.Tests.Handlers;

public class ShensAuctionHandlerTests : BaseHandlerTest<ShensAuctionHandler>
{
	[Fact]
	public void RunTestCases() {
		var testCases = new List<HandlerTestCase> {
			new() {
				Description = "Applies correctly",
				Item = new {
					SkyblockId = "CLOVER_HELMET",
					Attributes = new { Extra = new { auction = 6, bid = 6, price = "2500000000" } },
					BasePrice = 100,
					Price = 100
				},
				Prices = new Dictionary<string, double>(),
				ShouldApply = true,
				ExpectedPriceChange = 2500000000 * NetworthConstants.ApplicationWorth.ShensAuctionPrice - 100,
				ExpectedCalculation = new List<NetworthCalculation> {
					new() {
						Id = "CLOVER_HELMET",
						Type = "SHENS_AUCTION",
						Value = 2500000000 * NetworthConstants.ApplicationWorth.ShensAuctionPrice,
						Count = 1
					}
				}
			},
			new() {
				Description = "Does not apply (missing keys)",
				Item = new {
					SkyblockId = "RANDOM_ITEM",
					Attributes = new { Extra = new { price = "1000000" } },
					BasePrice = 100,
					Price = 100
				},
				Prices = new Dictionary<string, double>(),
				ShouldApply = false
			}
		};

		RunTests(testCases);
	}
}