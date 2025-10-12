using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Parsers.Farming;

namespace EliteAPI.Tests.EventProgressTests;

public class InitialCountersTests {
	private readonly List<ItemDto> _tools = [
		new() {
			SkyblockId = "THEORETICAL_HOE_WARTS_3",
			Uuid = "103d2e1f-0351-429f-b116-c85e81886597",
			Attributes = new Dictionary<string, string> {
				{ "modifier", "bountiful" },
				{ "mined_crops", "123" },
				{ "farmed_cultivating", "456" }
			}
		},

		new() {
			SkyblockId = "THEORETICAL_HOE_WARTS_3",
			Uuid = "103d2e1f-0351-429f-b116-c85e81886598",
			Attributes = new Dictionary<string, string> {
				{ "modifier", "bountiful" },
				{ "mined_crops", "123121" },
				{ "farmed_cultivating", "456" }
			}
		},

		new() {
			SkyblockId = "FUNGI_CUTTER",
			Uuid = "ab2472fa-7cb4-4b7b-b8e4-3158b1144569",
			Attributes = new Dictionary<string, string> {
				{ "modifier", "bountiful" },
				{ "farmed_cultivating", "7" }
			}
		}
	];

	private readonly List<ItemDto> _tools2 = [
		new() {
			SkyblockId = "THEORETICAL_HOE_WARTS_3",
			Uuid = "103d2e1f-0351-429f-b116-c85e81886597",
			Attributes = new Dictionary<string, string> {
				{ "modifier", "bountiful" },
				{ "mined_crops", "1248372" },
				{ "farmed_cultivating", "1278212" }
			}
		},

		new() {
			SkyblockId = "COCO_CHOPPER",
			Uuid = "d2d60faf-e820-419e-b0c8-d5eca8cc83ac",
			Attributes = new Dictionary<string, string> {
				{ "modifier", "bountiful" },
				{ "farmed_cultivating", "90123121" }
			}
		},

		new() {
			SkyblockId = "FUNGI_CUTTER",
			Uuid = "ab2472fa-7cb4-4b7b-b8e4-3158b1144569",
			Attributes = new Dictionary<string, string> {
				{ "modifier", "bountiful" },
				{ "farmed_cultivating", "7" }
			}
		}
	];

	[Fact]
	public void AddMoreToolsInitialCountersTest() {
		var initial = _tools.ExtractToolCounters();
		var extracted = _tools2.ExtractToolCounters(initial);

		// 1 tool was added
		extracted.Count.ShouldBe(8);

		// Initial tools should be the same
		extracted["103d2e1f-0351-429f-b116-c85e81886597"].ShouldBe(123);
		extracted["103d2e1f-0351-429f-b116-c85e81886597-c"].ShouldBe(456);
		extracted["103d2e1f-0351-429f-b116-c85e81886598"].ShouldBe(123121);
		extracted["103d2e1f-0351-429f-b116-c85e81886598-c"].ShouldBe(456);
		extracted["ab2472fa-7cb4-4b7b-b8e4-3158b1144569"].ShouldBe(0);
		extracted["ab2472fa-7cb4-4b7b-b8e4-3158b1144569-c"].ShouldBe(7);

		// New tool
		extracted["d2d60faf-e820-419e-b0c8-d5eca8cc83ac"].ShouldBe(0);
		extracted["d2d60faf-e820-419e-b0c8-d5eca8cc83ac-c"].ShouldBe(90123121);
	}

	[Fact]
	public void GetInitialCountersTest() {
		var extracted = _tools.ExtractToolCounters();

		extracted.Count.ShouldBe(6);

		extracted["103d2e1f-0351-429f-b116-c85e81886597"].ShouldBe(123);
		extracted["103d2e1f-0351-429f-b116-c85e81886597-c"].ShouldBe(456);
		extracted["103d2e1f-0351-429f-b116-c85e81886598"].ShouldBe(123121);
		extracted["103d2e1f-0351-429f-b116-c85e81886598-c"].ShouldBe(456);
		extracted["ab2472fa-7cb4-4b7b-b8e4-3158b1144569"].ShouldBe(0);
		extracted["ab2472fa-7cb4-4b7b-b8e4-3158b1144569-c"].ShouldBe(7);
	}

	[Fact]
	public void RemovedToolsTest() {
		var initial = _tools2.ExtractToolCounters();

		initial.Count.ShouldBe(6);

		initial["103d2e1f-0351-429f-b116-c85e81886597"].ShouldBe(1248372);
		initial["103d2e1f-0351-429f-b116-c85e81886597-c"].ShouldBe(1278212);
		initial["d2d60faf-e820-419e-b0c8-d5eca8cc83ac"].ShouldBe(0);
		initial["d2d60faf-e820-419e-b0c8-d5eca8cc83ac-c"].ShouldBe(90123121);

		// Emulate COCO_CHOPPER being removed
		initial.RemoveMissingTools(_tools);

		initial.Count.ShouldBe(4);

		initial["103d2e1f-0351-429f-b116-c85e81886597"].ShouldBe(1248372);
		initial["103d2e1f-0351-429f-b116-c85e81886597-c"].ShouldBe(1278212);

		// COCO_CHOPPER should be removed
		initial.ContainsKey("d2d60faf-e820-419e-b0c8-d5eca8cc83ac").ShouldBeFalse();
		initial.ContainsKey("d2d60faf-e820-419e-b0c8-d5eca8cc83ac-c").ShouldBeFalse();
	}
}