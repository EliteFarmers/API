using HypixelAPI.Networth.Calculators.Handlers;
using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;
using HypixelAPI.Networth.Tests.Infrastructure;

namespace HypixelAPI.Networth.Tests.Handlers;

public class EnrichmentHandlerTests : BaseHandlerTest<EnrichmentHandler>
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
                    SkyblockId = "ARTIFACT_OF_CONTROL",
                    Attributes = new { Extra = new { talisman_enrichment = "magic_find" } },
                    Price = 100
                },
                Prices = new Dictionary<string, double> { { "TALISMAN_ENRICHMENT_MAGIC_FIND", 9000000 }, { "TALISMAN_ENRICHMENT_CRITICAL_CHANCE", 8000000 } },
                ShouldApply = true,
                ExpectedPriceChange = 8000000 * NetworthConstants.ApplicationWorth.Enrichment,
                ExpectedCalculation = new List<NetworthCalculation>
                {
                    new()
                    {
                        Id = "MAGIC_FIND",
                        Type = "TALISMAN_ENRICHMENT",
                        Value = 8000000 * NetworthConstants.ApplicationWorth.Enrichment,
                        Count = 1
                    }
                }
            },
            new()
            {
                Description = "Does not apply",
                Item = new
                {
                    SkyblockId = "ARTIFACT_OF_CONTROL",
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
