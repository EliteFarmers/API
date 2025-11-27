using HypixelAPI.Networth.Calculators.Handlers;
using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;
using HypixelAPI.Networth.Tests.Infrastructure;
using Xunit;

namespace HypixelAPI.Networth.Tests.Handlers;

public class ItemEnchantmentsHandlerTests : BaseHandlerTest<EnchantmentHandler>
{
    [Fact]
    public void RunTestCases()
    {
        var testCases = new List<HandlerTestCase>
        {
            new()
            {
                Description = "Applies correctly",
                Item = new
                {
                    SkyblockId = "ROTTEN_LEGGINGS",
                    Enchantments = new Dictionary<string, int>
                    {
                        { "true_protection", 1 },
                        { "ultimate_legion", 5 },
                        { "rejuvenate", 5 },
                        { "growth", 6 },
                        { "protection", 5 }
                    },
                    Price = 100
                },
                Prices = new Dictionary<string, double>
                {
                    { "ENCHANTMENT_TRUE_PROTECTION_1", 1000000 },
                    { "ENCHANTMENT_ULTIMATE_LEGION_5", 40000000 },
                    { "ENCHANTMENT_REJUVENATE_5", 450000 },
                    { "ENCHANTMENT_GROWTH_6", 3000000 }
                },
                ShouldApply = true,
                ExpectedPriceChange = 
                    1000000 * NetworthConstants.ApplicationWorth.Enchantments +
                    40000000 * NetworthConstants.ApplicationWorth.Enchantments +
                    450000 * NetworthConstants.ApplicationWorth.Enchantments +
                    3000000 * NetworthConstants.ApplicationWorth.Enchantments,
                ExpectedCalculation = new List<NetworthCalculation>
                {
                    new() { Id = "ENCHANTMENT_TRUE_PROTECTION_1", Type = "ENCHANT", Value = 1000000 * NetworthConstants.ApplicationWorth.Enchantments, Count = 1 },
                    new() { Id = "ENCHANTMENT_ULTIMATE_LEGION_5", Type = "ENCHANT", Value = 40000000 * NetworthConstants.ApplicationWorth.Enchantments, Count = 1 },
                    new() { Id = "ENCHANTMENT_REJUVENATE_5", Type = "ENCHANT", Value = 450000 * NetworthConstants.ApplicationWorth.Enchantments, Count = 1 },
                    new() { Id = "ENCHANTMENT_GROWTH_6", Type = "ENCHANT", Value = 3000000 * NetworthConstants.ApplicationWorth.Enchantments, Count = 1 }
                }
            },
            new()
            {
                Description = "Applies correctly with blocked item-specific enchantment",
                Item = new
                {
                    SkyblockId = "ADVANCED_GARDENING_HOE",
                    Enchantments = new Dictionary<string, int>
                    {
                        { "replenish", 1 },
                        { "turbo_cane", 1 }
                    },
                    Price = 100
                },
                Prices = new Dictionary<string, double>
                {
                    { "ENCHANTMENT_REPLENISH_1", 1500000 },
                    { "ENCHANTMENT_TURBO_CANE_1", 5000 }
                },
                ShouldApply = true,
                ExpectedPriceChange = 5000 * NetworthConstants.ApplicationWorth.Enchantments,
                ExpectedCalculation = new List<NetworthCalculation>
                {
                    new() { Id = "ENCHANTMENT_TURBO_CANE_1", Type = "ENCHANT", Value = 5000 * NetworthConstants.ApplicationWorth.Enchantments, Count = 1 }
                }
            },
            new()
            {
                Description = "Applies correctly with ignored enchantment",
                Item = new
                {
                    SkyblockId = "IRON_SWORD",
                    Enchantments = new Dictionary<string, int>
                    {
                        { "scavenger", 5 },
                        { "smite", 6 }
                    },
                    Price = 100
                },
                Prices = new Dictionary<string, double>
                {
                    { "ENCHANTMENT_SCAVENGER_5", 300000 },
                    { "ENCHANTMENT_SMITE_6", 10 }
                },
                ShouldApply = true,
                ExpectedPriceChange = 10 * NetworthConstants.ApplicationWorth.Enchantments,
                ExpectedCalculation = new List<NetworthCalculation>
                {
                    new() { Id = "ENCHANTMENT_SMITE_6", Type = "ENCHANT", Value = 10 * NetworthConstants.ApplicationWorth.Enchantments, Count = 1 }
                }
            },
            new()
            {
                Description = "Applies correctly with stacking enchantment",
                Item = new
                {
                    SkyblockId = "DIVAN_DRILL",
                    Enchantments = new Dictionary<string, int> { { "compact", 10 } },
                    Price = 100
                },
                Prices = new Dictionary<string, double> { { "ENCHANTMENT_COMPACT_1", 6000000 } },
                ShouldApply = true,
                ExpectedPriceChange = 6000000 * NetworthConstants.ApplicationWorth.Enchantments,
                ExpectedCalculation = new List<NetworthCalculation>
                {
                    new() { Id = "ENCHANTMENT_COMPACT_1", Type = "ENCHANT", Value = 6000000 * NetworthConstants.ApplicationWorth.Enchantments, Count = 1 }
                }
            },
            new()
            {
                Description = "Applies correctly with silex",
                Item = new
                {
                    SkyblockId = "DIAMOND_PICKAXE",
                    Enchantments = new Dictionary<string, int> { { "efficiency", 10 } },
                    Price = 100
                },
                Prices = new Dictionary<string, double> { { "SIL_EX", 4500000 } },
                ShouldApply = true,
                ExpectedPriceChange = 5 * 4500000 * NetworthConstants.ApplicationWorth.Silex,
                ExpectedCalculation = new List<NetworthCalculation>
                {
                    new() { Id = "SIL_EX", Type = "SILEX", Value = 5 * 4500000 * NetworthConstants.ApplicationWorth.Silex, Count = 5 }
                }
            },
            new()
            {
                Description = "Applies correctly with golden bounty",
                Item = new
                {
                    SkyblockId = "IRON_SWORD",
                    Enchantments = new Dictionary<string, int> { { "scavenger", 6 } },
                    Price = 100
                },
                Prices = new Dictionary<string, double> { { "GOLDEN_BOUNTY", 30000000 } },
                ShouldApply = true,
                ExpectedPriceChange = 30000000 * NetworthConstants.ApplicationWorth.EnchantmentUpgrades,
                ExpectedCalculation = new List<NetworthCalculation>
                {
                    new() { Id = "GOLDEN_BOUNTY", Type = "ENCHANTMENT_UPGRADE", Value = 30000000 * NetworthConstants.ApplicationWorth.EnchantmentUpgrades, Count = 1 }
                }
            },
            new()
            {
                Description = "Does not apply (no enchantments)",
                Item = new
                {
                    SkyblockId = "IRON_SWORD",
                    Enchantments = new Dictionary<string, int>(),
                    Price = 100
                },
                Prices = new Dictionary<string, double>(),
                ShouldApply = false
            }
        };

        RunTests(testCases);
    }
}
