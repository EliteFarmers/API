using EliteAPI.Parsers.Farming;
using EliteAPI.Parsers.Profiles;
using EliteFarmers.HypixelAPI.DTOs;

namespace EliteAPI.Tests.ParserTests;

public class GreenhouseSlotParserTests
{
	[Fact]
	public void EncodeDecode_ShouldRoundTripTrackedSlots() {
		var input = new List<GreenhouseSlotUnlock> {
			new() { X = 0, Z = 0 },
			new() { X = 9, Z = 9 },
			new() { X = 2, Z = 7 },
			new() { X = 7, Z = 2 }
		};

		var (low, high) = GreenhouseSlotParser.EncodeSlots(input);
		var decoded = GreenhouseSlotParser.DecodeSlots(low, high);

		decoded.Count.ShouldBe(input.Count);
		decoded.ShouldContain(slot => slot.X == 0 && slot.Z == 0);
		decoded.ShouldContain(slot => slot.X == 9 && slot.Z == 9);
		decoded.ShouldContain(slot => slot.X == 2 && slot.Z == 7);
		decoded.ShouldContain(slot => slot.X == 7 && slot.Z == 2);
	}

	[Fact]
	public void EncodeSlots_ShouldIgnoreDefaultCenterPlusSlots() {
		var input = new List<GreenhouseSlotUnlock> {
			new() { X = 4, Z = 4 }, // center default
			new() { X = 5, Z = 3 }, // north arm default
			new() { X = 3, Z = 5 }, // west arm default
			new() { X = 0, Z = 0 }  // tracked
		};

		var (low, high) = GreenhouseSlotParser.EncodeSlots(input);
		var decoded = GreenhouseSlotParser.DecodeSlots(low, high);

		decoded.Count.ShouldBe(1);
		decoded[0].X.ShouldBe(0);
		decoded[0].Z.ShouldBe(0);
	}

	[Fact]
	public void EncodeSlots_ShouldTrack88SlotsOnly() {
		var allGridSlots = new List<GreenhouseSlotUnlock>();
		for (var z = 0; z < 10; z++) {
			for (var x = 0; x < 10; x++) {
				allGridSlots.Add(new GreenhouseSlotUnlock { X = x, Z = z });
			}
		}

		var (low, high) = GreenhouseSlotParser.EncodeSlots(allGridSlots);
		var decoded = GreenhouseSlotParser.DecodeSlots(low, high);

		decoded.Count.ShouldBe(88);
	}
}

public class MemberDataParserTests
{
	[Fact]
	public void ExtractMemberData_ShouldBuildMutationFlagsAndNewFields() {
		var incoming = new ProfileMemberResponse {
			Attributes = new ProfileMemberAttributes {
				Stacks = new Dictionary<string, int> {
					{ "THUNDER_SHARDS", 32 }
				}
			},
			Shards = new ProfileMemberShards {
				Owned = [
					new ProfileMemberShard {
						Type = "THUNDER",
						Owned = 12,
						CapturedAt = 1234567890
					}
				]
			},
			PlayerData = new RawMemberPlayerData {
				GardenChips = new ProfileMemberGardenChips {
					Cropshot = 1,
					Rarefinder = 4
				}
			},
			Garden = new GardenPlayerDataResponse {
				AnalyzedGreenhouseCrops = [ "alpha", "beta" ],
				DiscoveredGreenhouseCrops = [ "beta", "gamma" ]
			}
		};

		var parsed = incoming.ExtractMemberData();

		parsed.AttributeStacks["THUNDER_SHARDS"].ShouldBe(32);
		parsed.Shards.Count.ShouldBe(1);
		parsed.Shards[0].Type.ShouldBe("THUNDER");
		parsed.Shards[0].AmountOwned.ShouldBe(12);
		parsed.Shards[0].CapturedAt.ShouldBe(1234567890);

		parsed.GardenChips.Cropshot.ShouldBe(1);
		parsed.GardenChips.Rarefinder.ShouldBe(4);

		parsed.Mutations.Count.ShouldBe(3);
		parsed.Mutations["alpha"].Analyzed.ShouldBeTrue();
		parsed.Mutations["alpha"].Discovered.ShouldBeFalse();

		parsed.Mutations["beta"].Analyzed.ShouldBeTrue();
		parsed.Mutations["beta"].Discovered.ShouldBeTrue();

		parsed.Mutations["gamma"].Analyzed.ShouldBeFalse();
		parsed.Mutations["gamma"].Discovered.ShouldBeTrue();
	}
}
