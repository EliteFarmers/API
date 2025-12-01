using HypixelAPI.Networth.Calculators.Handlers;
using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;
using HypixelAPI.Networth.Tests.Infrastructure;

namespace HypixelAPI.Networth.Tests.Handlers;

public class GemstonesHandlerTests : BaseHandlerTest<GemstonesHandler>
{
	[Fact]
	public void RunTestCases() {
		var testCases = new List<HandlerTestCase> {
			new() {
				Description = "Applies correctly v1",
				Item = new {
					SkyblockId = "HYPERION",
					GemstoneSlots = new[] {
						new {
							SlotType = "SAPPHIRE",
							Costs = new object[] {
								new { Type = "COINS", Coins = 250000.0 },
								new { Type = "ITEM", ItemId = "FLAWLESS_SAPPHIRE_GEM", Amount = 4 }
							}
						},
						new {
							SlotType = "COMBAT", Costs = new object[] {
								new { Type = "COINS", Coins = 250000.0 },
								new { Type = "ITEM", ItemId = "FLAWLESS_JASPER_GEM", Amount = 1 },
								new { Type = "ITEM", ItemId = "FLAWLESS_SAPPHIRE_GEM", Amount = 1 },
								new { Type = "ITEM", ItemId = "FLAWLESS_RUBY_GEM", Amount = 1 },
								new { Type = "ITEM", ItemId = "FLAWLESS_AMETHYST_GEM", Amount = 1 }
							}
						}
					},
					Gems = new Dictionary<string, string?> {
						{ "COMBAT_0", "PERFECT" },
						{ "SAPPHIRE_0", "PERFECT" },
						{ "COMBAT_0_gem", "SAPPHIRE" }
					},
					Price = 100
				},
				Prices = new Dictionary<string, double> { { "PERFECT_SAPPHIRE_GEM", 16000000 } },
				ShouldApply = true,
				ExpectedPriceChange = 2 * 16000000 * NetworthConstants.ApplicationWorth.Gemstone,
				ExpectedCalculation = new List<NetworthCalculation> {
					new() {
						Id = "PERFECT_SAPPHIRE_GEM", Type = "GEMSTONE",
						Value = 16000000 * NetworthConstants.ApplicationWorth.Gemstone, Count = 1
					},
					new() {
						Id = "PERFECT_SAPPHIRE_GEM", Type = "GEMSTONE",
						Value = 16000000 * NetworthConstants.ApplicationWorth.Gemstone, Count = 1
					}
				}
			},
			new() {
				Description = "Applies correctly with divan",
				Item = new {
					SkyblockId = "DIVAN_CHESTPLATE",
					GemstoneSlots = new[] {
						new {
							SlotType = "AMBER",
							Costs = new[] { new { Type = "ITEM", ItemId = "GEMSTONE_CHAMBER", Amount = 1 } }
						},
						new {
							SlotType = "JADE",
							Costs = new[] { new { Type = "ITEM", ItemId = "GEMSTONE_CHAMBER", Amount = 1 } }
						},
						new {
							SlotType = "AMBER",
							Costs = new[] { new { Type = "ITEM", ItemId = "GEMSTONE_CHAMBER", Amount = 1 } }
						},
						new {
							SlotType = "JADE",
							Costs = new[] { new { Type = "ITEM", ItemId = "GEMSTONE_CHAMBER", Amount = 1 } }
						},
						new {
							SlotType = "TOPAZ",
							Costs = new[] { new { Type = "ITEM", ItemId = "GEMSTONE_CHAMBER", Amount = 1 } }
						}
					},
					Gems = new Dictionary<string, string?> {
						{ "JADE_1", "PERFECT" },
						{ "JADE_0", "PERFECT" },
						{ "AMBER_0", "PERFECT" },
						{ "AMBER_1", "PERFECT" },
						{ "TOPAZ_0", "PERFECT" }
					},
					UnlockedSlots = new List<string> { "TOPAZ_0", "JADE_1", "JADE_0", "AMBER_0", "AMBER_1" },
					Price = 100
				},
				Prices = new Dictionary<string, double> {
					{ "GEMSTONE_CHAMBER", 7000000 },
					{ "PERFECT_AMBER_GEM", 15000000 },
					{ "PERFECT_JADE_GEM", 16000000 },
					{ "PERFECT_TOPAZ_GEM", 17500000 }
				},
				ShouldApply = true,
				ExpectedPriceChange = 5 * 7000000 * NetworthConstants.ApplicationWorth.GemstoneChambers +
				                      2 * 16000000 * NetworthConstants.ApplicationWorth.Gemstone +
				                      2 * 15000000 * NetworthConstants.ApplicationWorth.Gemstone +
				                      17500000 * NetworthConstants.ApplicationWorth.Gemstone,
				ExpectedCalculation = new List<NetworthCalculation> {
					new() {
						Id = "AMBER", Type = "GEMSTONE_SLOT",
						Value = 7000000 * NetworthConstants.ApplicationWorth.GemstoneChambers, Count = 1
					},
					new() {
						Id = "JADE", Type = "GEMSTONE_SLOT",
						Value = 7000000 * NetworthConstants.ApplicationWorth.GemstoneChambers, Count = 1
					},
					new() {
						Id = "AMBER", Type = "GEMSTONE_SLOT",
						Value = 7000000 * NetworthConstants.ApplicationWorth.GemstoneChambers, Count = 1
					},
					new() {
						Id = "JADE", Type = "GEMSTONE_SLOT",
						Value = 7000000 * NetworthConstants.ApplicationWorth.GemstoneChambers, Count = 1
					},
					new() {
						Id = "TOPAZ", Type = "GEMSTONE_SLOT",
						Value = 7000000 * NetworthConstants.ApplicationWorth.GemstoneChambers, Count = 1
					},
					new() {
						Id = "PERFECT_AMBER_GEM", Type = "GEMSTONE",
						Value = 15000000 * NetworthConstants.ApplicationWorth.Gemstone, Count = 1
					},
					new() {
						Id = "PERFECT_JADE_GEM", Type = "GEMSTONE",
						Value = 16000000 * NetworthConstants.ApplicationWorth.Gemstone, Count = 1
					},
					new() {
						Id = "PERFECT_AMBER_GEM", Type = "GEMSTONE",
						Value = 15000000 * NetworthConstants.ApplicationWorth.Gemstone, Count = 1
					},
					new() {
						Id = "PERFECT_JADE_GEM", Type = "GEMSTONE",
						Value = 16000000 * NetworthConstants.ApplicationWorth.Gemstone, Count = 1
					},
					new() {
						Id = "PERFECT_TOPAZ_GEM", Type = "GEMSTONE",
						Value = 17500000 * NetworthConstants.ApplicationWorth.Gemstone, Count = 1
					}
				}
			}
		};

		RunTests(testCases);
	}
}