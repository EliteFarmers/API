using HypixelAPI.Networth.Calculators.Handlers;
using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;
using HypixelAPI.Networth.Tests.Infrastructure;

namespace HypixelAPI.Networth.Tests.Handlers;

public class PotatoBooksHandlerTests : BaseHandlerTest<PotatoBooksHandler>
{
	[Fact]
	public void RunTestCases() {
		var testCases = new List<HandlerTestCase> {
			new() {
				Description = "Applies correctly",
				Item = new {
					SkyblockId = "IRON_SWORD",
					Attributes = new { Extra = new { hot_potato_count = 10 } },
					Price = 100
				},
				Prices = new Dictionary<string, double>
					{ { "HOT_POTATO_BOOK", 80000 }, { "FUMING_POTATO_BOOK", 1400000 } },
				ShouldApply = true,
				ExpectedPriceChange = 10 * 80000 * NetworthConstants.ApplicationWorth.HotPotatoBook,
				ExpectedCalculation = new List<NetworthCalculation> {
					new() {
						Id = "HOT_POTATO_BOOK",
						Type = "HOT_POTATO_BOOK",
						Value = 10 * 80000 * NetworthConstants.ApplicationWorth.HotPotatoBook,
						Count = 10
					}
				}
			},
			new() {
				Description = "Applies correctly with Fuming Potato Books",
				Item = new {
					SkyblockId = "IRON_SWORD",
					Attributes = new { Extra = new { hot_potato_count = 15 } },
					Price = 100
				},
				Prices = new Dictionary<string, double>
					{ { "HOT_POTATO_BOOK", 80000 }, { "FUMING_POTATO_BOOK", 1400000 } },
				ShouldApply = true,
				ExpectedPriceChange = 10 * 80000 * NetworthConstants.ApplicationWorth.HotPotatoBook +
				                      5 * 1400000 * NetworthConstants.ApplicationWorth.FumingPotatoBook,
				ExpectedCalculation = new List<NetworthCalculation> {
					new() {
						Id = "FUMING_POTATO_BOOK",
						Type = "FUMING_POTATO_BOOK",
						Value = 5 * 1400000 * NetworthConstants.ApplicationWorth.FumingPotatoBook,
						Count = 5
					},
					new() {
						Id = "HOT_POTATO_BOOK",
						Type = "HOT_POTATO_BOOK",
						Value = 10 * 80000 * NetworthConstants.ApplicationWorth.HotPotatoBook,
						Count = 10
					}
				}
			},
			new() {
				Description = "Does not apply",
				Item = new {
					SkyblockId = "IRON_SWORD",
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