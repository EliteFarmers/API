using HypixelAPI.Networth.Calculators.Handlers;
using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;
using HypixelAPI.Networth.Tests.Infrastructure;

namespace HypixelAPI.Networth.Tests.Handlers;

public class EnchantedBookHandlerTests : BaseHandlerTest<EnchantedBookHandler>
{
    [Fact]
    public void RunTestCases()
    {
        var testCases = new List<HandlerTestCase>
        {
            new()
            {
                Description = "Applies correctly with single enchantment",
                Item = new
                {
                    SkyblockId = "ENCHANTED_BOOK",
                    Attributes = new { Extra = new { enchantments = new { ultimate_legion = 7 } } },
                    BasePrice = 0,
                    Price = 100
                },
                Prices = new Dictionary<string, double> { { "ENCHANTMENT_ULTIMATE_LEGION_7", 50000000 } },
                ShouldApply = true,
                ExpectedNewBasePrice = 50000000,
                ExpectedCalculation = new List<NetworthCalculation>
                {
                    new()
                    {
                        Id = "ULTIMATE_LEGION_7",
                        Type = "ENCHANT",
                        Value = 50000000,
                        Count = 1
                    }
                }
            },
            new()
            {
                Description = "Applies correctly with mutliple enchantment",
                Item = new
                {
                    SkyblockId = "ENCHANTED_BOOK",
                    Attributes = new { Extra = new { enchantments = new { ultimate_legion = 7, smite = 7 } } },
                    BasePrice = 0,
                    Price = 100
                },
                Prices = new Dictionary<string, double> { { "ENCHANTMENT_ULTIMATE_LEGION_7", 50000000 }, { "ENCHANTMENT_SMITE_7", 4000000 } },
                ShouldApply = true,
                ExpectedNewBasePrice = 50000000 * NetworthConstants.ApplicationWorth.Enchantments + 4000000 * NetworthConstants.ApplicationWorth.Enchantments,
                ExpectedCalculation = new List<NetworthCalculation>
                {
                    new()
                    {
                        Id = "ULTIMATE_LEGION_7",
                        Type = "ENCHANT",
                        Value = 50000000 * NetworthConstants.ApplicationWorth.Enchantments,
                        Count = 1
                    },
                    new()
                    {
                        Id = "SMITE_7",
                        Type = "ENCHANT",
                        Value = 4000000 * NetworthConstants.ApplicationWorth.Enchantments,
                        Count = 1
                    }
                }
            },
            new()
            {
                Description = "Applies correctly with no price",
                Item = new
                {
                    SkyblockId = "ENCHANTED_BOOK",
                    Attributes = new { Extra = new { enchantments = new { smite = 5 } } },
                    BasePrice = 0,
                    Price = 100
                },
                Prices = new Dictionary<string, double>(),
                ShouldApply = true,
                ExpectedNewBasePrice = 0,
                ExpectedCalculation = new List<NetworthCalculation>()
            },
            new()
            {
                Description = "Does not apply (not ENCHANTED_BOOK)",
                Item = new
                {
                    SkyblockId = "IRON_SWORD",
                    Attributes = new { Extra = new { enchantments = new { sharpness = 5 } } },
                    Price = 100
                },
                Prices = new Dictionary<string, double>(),
                ShouldApply = false
            },
            new()
            {
                Description = "Does not apply (no enchantments)",
                Item = new
                {
                    SkyblockId = "ENCHANTED_BOOK",
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
