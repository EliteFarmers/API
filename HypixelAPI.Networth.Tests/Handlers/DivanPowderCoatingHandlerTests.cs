using HypixelAPI.Networth.Calculators.Handlers;
using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;
using HypixelAPI.Networth.Tests.Infrastructure;

namespace HypixelAPI.Networth.Tests.Handlers;

public class DivanPowderCoatingHandlerTests : BaseHandlerTest<DivanPowderCoatingHandler>
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
                    SkyblockId = "DIVAN_DRILL",
                    Attributes = new { Extra = new { divan_powder_coating = 1 } },
                    Price = 100
                },
                Prices = new Dictionary<string, double> { { "DIVAN_POWDER_COATING", 100000000 } },
                ShouldApply = true,
                ExpectedPriceChange = 100000000 * NetworthConstants.ApplicationWorth.DivanPowderCoating,
                ExpectedCalculation = new List<NetworthCalculation>
                {
                    new()
                    {
                        Id = "DIVAN_POWDER_COATING",
                        Type = "DIVAN_POWDER_COATING",
                        Value = 100000000 * NetworthConstants.ApplicationWorth.DivanPowderCoating,
                        Count = 1
                    }
                }
            },
            new()
            {
                Description = "Does not apply",
                Item = new
                {
                    SkyblockId = "DIVAN_DRILL",
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
