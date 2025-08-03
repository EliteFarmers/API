using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Events;
using EliteAPI.Parsers.Farming;

namespace EliteAPI.Tests.EventProgressTests; 

public class ToolStateTests {
    private readonly List<ItemDto> _tools =
    [
        new ItemDto
        {
            SkyblockId = "THEORETICAL_HOE_WARTS_3",
            Uuid = "103d2e1f-0351-429f-b116-c85e81886597",
            Attributes = new Dictionary<string, string>
            {
                { "modifier", "bountiful" },
                { "mined_crops", "123" },
                { "farmed_cultivating", "456" }
            }
        },

        new ItemDto
        {
            SkyblockId = "THEORETICAL_HOE_WARTS_3",
            Uuid = "103d2e1f-0351-429f-b116-c85e81886598",
            Attributes = new Dictionary<string, string>
            {
                { "modifier", "bountiful" },
                { "mined_crops", "123121" },
                { "farmed_cultivating", "456" }
            }
        },

        new ItemDto
        {
            SkyblockId = "FUNGI_CUTTER",
            Uuid = "ab2472fa-7cb4-4b7b-b8e4-3158b1144569",
            Attributes = new Dictionary<string, string>
            {
                { "modifier", "bountiful" },
                { "farmed_cultivating", "7" }
            }
        }
    ];
    
    private readonly List<ItemDto> _tools2 =
    [
        new ItemDto
        {
            SkyblockId = "THEORETICAL_HOE_WARTS_3",
            Uuid = "103d2e1f-0351-429f-b116-c85e81886597",
            Attributes = new Dictionary<string, string>
            {
                { "modifier", "bountiful" },
                { "mined_crops", "1248372" },
                { "farmed_cultivating", "1278212" }
            }
        },

        new ItemDto
        {
            SkyblockId = "COCO_CHOPPER",
            Uuid = "d2d60faf-e820-419e-b0c8-d5eca8cc83ac",
            Attributes = new Dictionary<string, string>
            {
                { "modifier", "bountiful" },
                { "farmed_cultivating", "90123121" }
            }
        },

        new ItemDto
        {
            SkyblockId = "FUNGI_CUTTER",
            Uuid = "ab2472fa-7cb4-4b7b-b8e4-3158b1144569",
            Attributes = new Dictionary<string, string>
            {
                { "modifier", "bountiful" },
                { "farmed_cultivating", "7" }
            }
        }
    ];
    
    [Fact]
    public void ToolStatesExtractionTest() {
        var actual = _tools.ExtractToolStates();

        actual.Count.ShouldBe(3);
        
        actual.ShouldContainKey("103d2e1f-0351-429f-b116-c85e81886597");
        actual.ShouldContainKey("103d2e1f-0351-429f-b116-c85e81886598");
        actual.ShouldContainKey("ab2472fa-7cb4-4b7b-b8e4-3158b1144569");
        
        actual.Values.ShouldAllBe(s => s.IsActive);
        
        actual["103d2e1f-0351-429f-b116-c85e81886597"].Counter.Current.ShouldBe(123);
        actual["103d2e1f-0351-429f-b116-c85e81886597"].Counter.Initial.ShouldBe(123);
        actual["103d2e1f-0351-429f-b116-c85e81886597"].Cultivating.Current.ShouldBe(456);
        actual["103d2e1f-0351-429f-b116-c85e81886597"].Cultivating.Initial.ShouldBe(456);
        
        actual["103d2e1f-0351-429f-b116-c85e81886598"].Counter.Current.ShouldBe(123121);
        actual["103d2e1f-0351-429f-b116-c85e81886598"].Counter.Initial.ShouldBe(123121);
        actual["103d2e1f-0351-429f-b116-c85e81886598"].Cultivating.Current.ShouldBe(456);
        actual["103d2e1f-0351-429f-b116-c85e81886598"].Cultivating.Initial.ShouldBe(456);
        
        actual["ab2472fa-7cb4-4b7b-b8e4-3158b1144569"].Counter.Current.ShouldBe(0);
        actual["ab2472fa-7cb4-4b7b-b8e4-3158b1144569"].Counter.Initial.ShouldBe(0);
        actual["ab2472fa-7cb4-4b7b-b8e4-3158b1144569"].Cultivating.Current.ShouldBe(7);
        actual["ab2472fa-7cb4-4b7b-b8e4-3158b1144569"].Cultivating.Initial.ShouldBe(7);
    }

    [Fact]
    public void NewToolsAddedTest() {
        var initial = _tools.ExtractToolStates();
        var extracted = _tools2.ExtractToolStates(initial);
        
        // 1 tool was added
        extracted.Count.ShouldBe(4);
        
        // "103d2e1f-0351-429f-b116-c85e81886598" should be inactive
        extracted["103d2e1f-0351-429f-b116-c85e81886598"].IsActive.ShouldBeFalse();
        // Values should be same as initial 
        extracted["103d2e1f-0351-429f-b116-c85e81886598"].Counter.ShouldBe(initial["103d2e1f-0351-429f-b116-c85e81886598"].Counter);
        extracted["103d2e1f-0351-429f-b116-c85e81886598"].Cultivating.ShouldBe(initial["103d2e1f-0351-429f-b116-c85e81886598"].Cultivating);
        
        // New tool
        extracted["d2d60faf-e820-419e-b0c8-d5eca8cc83ac"].IsActive.ShouldBeTrue();
        extracted["d2d60faf-e820-419e-b0c8-d5eca8cc83ac"].Counter.Current.ShouldBe(0);
        extracted["d2d60faf-e820-419e-b0c8-d5eca8cc83ac"].Counter.Initial.ShouldBe(0);
        extracted["d2d60faf-e820-419e-b0c8-d5eca8cc83ac"].Cultivating.Current.ShouldBe(90123121);
        extracted["d2d60faf-e820-419e-b0c8-d5eca8cc83ac"].Cultivating.Initial.ShouldBe(90123121);
        
        // Initial tools should have increased counters
        extracted["103d2e1f-0351-429f-b116-c85e81886597"].Counter.Current.ShouldBe(1248372);
        extracted["103d2e1f-0351-429f-b116-c85e81886597"].Counter.Initial.ShouldBe(123);
        extracted["103d2e1f-0351-429f-b116-c85e81886597"].Cultivating.Current.ShouldBe(1278212);
        extracted["103d2e1f-0351-429f-b116-c85e81886597"].Cultivating.Initial.ShouldBe(456);
    }

    [Fact]
    public void ToolRemovedAndAddedWithProgressTest() {
        var initial = _tools.ExtractToolStates();
        var second = _tools2.ExtractToolStates(initial);
        second.Count.ShouldBe(4);
        
        // "103d2e1f-0351-429f-b116-c85e81886598" is currently inactive
        second["103d2e1f-0351-429f-b116-c85e81886598"].IsActive.ShouldBeFalse();
        
        // Add the tool back with progress
        var third = new List<ItemDto> {
            new() {
                SkyblockId = "THEORETICAL_HOE_WARTS_3",
                Uuid = "103d2e1f-0351-429f-b116-c85e81886598",
                Attributes = new Dictionary<string, string> {
                    { "modifier", "bountiful" },
                    { "mined_crops", "1248372" },
                    { "farmed_cultivating", "1278212" }
                }
            }
        }.ExtractToolStates(second);

        // Make sure a new tool wasn't added
        third.Count.ShouldBe(4);
        
        // "103d2e1f-0351-429f-b116-c85e81886598" should be active
        third["103d2e1f-0351-429f-b116-c85e81886598"].IsActive.ShouldBeTrue();
        
        // Check that the "Uncounted" values are correct
        third["103d2e1f-0351-429f-b116-c85e81886598"].Counter.Uncounted.ShouldBe(1248372 - 123121);
        third["103d2e1f-0351-429f-b116-c85e81886598"].Cultivating.Uncounted.ShouldBe(1278212 - 456);
        
        // Check that the "Current" values are correct
        third["103d2e1f-0351-429f-b116-c85e81886598"].Counter.Current.ShouldBe(1248372);
        third["103d2e1f-0351-429f-b116-c85e81886598"].Cultivating.Current.ShouldBe(1278212);
        
        // Check that the "Initial" values are correct
        third["103d2e1f-0351-429f-b116-c85e81886598"].Counter.Initial.ShouldBe(123121);
        third["103d2e1f-0351-429f-b116-c85e81886598"].Cultivating.Initial.ShouldBe(456);
        
        // Check that the increases are correct
        third["103d2e1f-0351-429f-b116-c85e81886598"].Counter.IncreaseFromInitial().ShouldBe(0);
        third["103d2e1f-0351-429f-b116-c85e81886598"].Cultivating.IncreaseFromInitial().ShouldBe(0);
    }
    
    [Fact]
    public void ToolCultivatingWentNegativeTest() {
        var first = new List<ItemDto> {
            new() {
                SkyblockId = "THEORETICAL_HOE_WARTS_3",
                Uuid = "103d2e1f-0351-429f-b116-c85e81886598",
                Attributes = new Dictionary<string, string> {
                    { "modifier", "bountiful" },
                    { "mined_crops", "1248372" },
                    { "farmed_cultivating", "2147483600" } // 47 away from int limit
                }
            }
        }.ExtractToolStates();

        first.Count.ShouldBe(1);
        first["103d2e1f-0351-429f-b116-c85e81886598"].Cultivating.Initial.ShouldBe(2147483600);
        
        var second = new List<ItemDto> {
            new() {
                SkyblockId = "THEORETICAL_HOE_WARTS_3",
                Uuid = "103d2e1f-0351-429f-b116-c85e81886598",
                Attributes = new Dictionary<string, string> {
                    { "modifier", "bountiful" },
                    { "mined_crops", "1248372" },
                    { "farmed_cultivating", "-2147483604" } // Wrapped around
                }
            }
        }.ExtractToolStates(first);
        
        second.Count.ShouldBe(1);
        second["103d2e1f-0351-429f-b116-c85e81886598"].IsActive.ShouldBeTrue();

        second["103d2e1f-0351-429f-b116-c85e81886598"].Cultivating.IncreaseFromInitial().ShouldBe(90);
    }
}