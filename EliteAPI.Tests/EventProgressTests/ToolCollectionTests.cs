using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Parsers.Farming;

namespace EliteAPI.Tests.EventProgressTests; 

public class ToolCollectionTests {
    
    [Fact]
    public void GetCollectedCropsFromTool() {
        var item = new ItemDto {
            SkyblockId = "THEORETICAL_HOE_WARTS_3",
            Uuid = "103d2e1f-0351-429f-b116-c85e81886597",
            Attributes = new Dictionary<string, string> {
                { "modifier", "bountiful" },
                { "mined_crops", "123" },
                { "farmed_cultivating", "456" }
            }
        };
        
        const int expected = 123;
        var actual = item.ExtractCollected();
        var cultivated = item.ExtractCultivating();
        
        actual.Should().Be(expected);
        cultivated.Should().Be(456);
    }
    
    [Fact]
    public void GetCultivatedCropsFromToolTest() {
        var item = new ItemDto {
            SkyblockId = "MELON_DICER",
            Uuid = "103d2e1f-0351-429f-b116-c85e81886597",
            Attributes = new Dictionary<string, string> {
                { "modifier", "bountiful" },
                { "farmed_cultivating", "456" }
            }
        };
        
        const int expected = 456;
        var actual = item.ExtractCollected();
        
        actual.Should().Be(expected);
    }
    
    [Fact]
    public void TryGetCultivatedCropsFromToolTest() {
        var item = new ItemDto {
            SkyblockId = "MELON_DICER",
            Uuid = "103d2e1f-0351-429f-b116-c85e81886597",
            Attributes = new Dictionary<string, string> {
                { "modifier", "bountiful" },
                { "farmed_cultivating", "invalid" }
            }
        };
        
        const int expected = 0;
        var actual = item.ExtractCollected();
        
        actual.Should().Be(expected);
    }
    
    [Fact]
    public void TryGetCollectedCropsFromToolTest() {
        var item = new ItemDto {
            SkyblockId = "MELON_DICER",
            Uuid = "103d2e1f-0351-429f-b116-c85e81886597",
            Attributes = new Dictionary<string, string> {
                { "modifier", "bountiful" },
            }
        };
        
        const int expected = 0;
        var actual = item.ExtractCollected();
        
        actual.Should().Be(expected);
    }
    
}