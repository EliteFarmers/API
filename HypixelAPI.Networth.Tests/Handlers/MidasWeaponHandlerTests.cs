using HypixelAPI.Networth.Calculators.Handlers;
using HypixelAPI.Networth.Models;
using HypixelAPI.Networth.Tests.Infrastructure;

namespace HypixelAPI.Networth.Tests.Handlers;

public class MidasWeaponHandlerTests : BaseHandlerTest<MidasWeaponHandler>
{
	[Fact]
	public void RunTestCases() {
		var testCases = new List<HandlerTestCase> {
			new() {
				Description = "Applies correctly less than max price paid",
				Item = new {
					SkyblockId = "MIDAS_SWORD",
					Attributes = new { Extra = new { winning_bid = 10000000 } },
					BasePrice = 100,
					Price = 100
				},
				Prices = new Dictionary<string, double>(),
				ShouldApply = true,
				ExpectedPriceChange = 0,
				ExpectedCalculation = new List<NetworthCalculation>()
			},
			new() {
				Description = "Applies correctly less than max price paid with additonal coins",
				Item = new {
					SkyblockId = "MIDAS_SWORD",
					Attributes = new { Extra = new { winning_bid = 20000000, additional_coins = 25000000 } },
					BasePrice = 100,
					Price = 100
				},
				Prices = new Dictionary<string, double>(),
				ShouldApply = true,
				ExpectedPriceChange = 0,
				ExpectedCalculation = new List<NetworthCalculation>()
			},
			new() {
				Description = "Applies correctly max price paid",
				Item = new {
					SkyblockId = "MIDAS_SWORD",
					Attributes = new { Extra = new { winning_bid = 50000000 } },
					BasePrice = 100,
					Price = 100
				},
				Prices = new Dictionary<string, double> { { "MIDAS_SWORD_50M", 300000000 } },
				ShouldApply = true,
				ExpectedNewBasePrice = 300000000,
				ExpectedCalculation = new List<NetworthCalculation> {
					new() {
						Id = "MIDAS_SWORD",
						Type = "MIDAS_SWORD_50M",
						Value = 300000000,
						Count = 1
					}
				}
			},
			new() {
				Description = "Applies correctly max price paid + additional coins",
				Item = new {
					SkyblockId = "MIDAS_STAFF",
					Attributes = new { Extra = new { winning_bid = 50000000, additional_coins = 50000000 } },
					BasePrice = 100,
					Price = 100
				},
				Prices = new Dictionary<string, double> { { "MIDAS_STAFF_100M", 400000000 } },
				ShouldApply = true,
				ExpectedNewBasePrice = 400000000,
				ExpectedCalculation = new List<NetworthCalculation> {
					new() {
						Id = "MIDAS_STAFF",
						Type = "MIDAS_STAFF_100M",
						Value = 400000000,
						Count = 1
					}
				}
			},
			new() {
				Description = "Applies correctly max price paid (Starred)",
				Item = new {
					SkyblockId = "STARRED_MIDAS_STAFF",
					Attributes = new { Extra = new { winning_bid = 50000000, additional_coins = 1000000000000 } },
					BasePrice = 100,
					Price = 100
				},
				Prices = new Dictionary<string, double> { { "STARRED_MIDAS_STAFF_500M", 580000000 } },
				ShouldApply = true,
				ExpectedNewBasePrice = 580000000,
				ExpectedCalculation = new List<NetworthCalculation> {
					new() {
						Id = "STARRED_MIDAS_STAFF",
						Type = "STARRED_MIDAS_STAFF_500M",
						Value = 580000000,
						Count = 1
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