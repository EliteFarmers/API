using HypixelAPI.Networth.Calculators.Handlers;
using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;
using HypixelAPI.Networth.Tests.Infrastructure;

namespace HypixelAPI.Networth.Tests.Handlers;

public class DyeHandlerTests : BaseHandlerTest<DyeHandler>
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
                    SkyblockId = "POWER_WITHER_LEGGINGS",
                    Attributes = new { Extra = new { dye_item = "DYE_WARDEN" } },
                    Price = 100
                },
                Prices = new Dictionary<string, double> { { "DYE_WARDEN", 90000000 } },
                ShouldApply = true,
                ExpectedPriceChange = 90000000 * NetworthConstants.ApplicationWorth.Dye,
                ExpectedCalculation = new List<NetworthCalculation>
                {
                    new()
                    {
                        Id = "DYE_WARDEN",
                        Type = "DYE",
                        Value = 90000000 * NetworthConstants.ApplicationWorth.Dye,
                        Count = 1
                    }
                }
            },
            new()
            {
                Description = "Does not apply",
                Item = new
                {
                    SkyblockId = "POWER_WITHER_LEGGINGS",
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
