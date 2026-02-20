using EliteAPI.Models.Entities.Hypixel;
using EliteFarmers.HypixelAPI.DTOs;

namespace EliteAPI.Parsers.Profiles;

public static class MemberDataParser
{
	public static ProfileMemberData ExtractMemberData(this ProfileMemberResponse incomingData) {
		var incomingChips = incomingData.PlayerData?.GardenChips;

		return new ProfileMemberData {
			AttributeStacks = incomingData.Attributes.Stacks ?? new Dictionary<string, int>(),
			Shards = incomingData.Shards.Owned
				.Select(shard => new ProfileMemberShardData {
					Type = shard.Type,
					AmountOwned = shard.Owned,
					CapturedAt = shard.CapturedAt
				})
				.ToList(),
			GardenChips = new ProfileMemberGardenChipsData {
				Cropshot = incomingChips?.Cropshot,
				Sowledge = incomingChips?.Sowledge,
				Hypercharge = incomingChips?.Hypercharge,
				Quickdraw = incomingChips?.Quickdraw,
				Mechamind = incomingChips?.Mechamind,
				Overdrive = incomingChips?.Overdrive,
				Synthesis = incomingChips?.Synthesis,
				VerminVaporizer = incomingChips?.VerminVaporizer,
				Evergreen = incomingChips?.Evergreen,
				Rarefinder = incomingChips?.Rarefinder
			},
			Mutations = BuildMutations(
				incomingData.Garden?.AnalyzedGreenhouseCrops,
				incomingData.Garden?.DiscoveredGreenhouseCrops)
		};
	}

	public static Dictionary<string, ProfileMemberMutationData> BuildMutations(
		IEnumerable<string>? analyzedMutations,
		IEnumerable<string>? discoveredMutations) {
		var analyzed = (analyzedMutations ?? []).ToHashSet(StringComparer.Ordinal);
		var discovered = (discoveredMutations ?? []).ToHashSet(StringComparer.Ordinal);
		var allKeys = analyzed.Union(discovered).ToList();

		var mutations = new Dictionary<string, ProfileMemberMutationData>(StringComparer.Ordinal);

		foreach (var mutation in allKeys) {
			mutations[mutation] = new ProfileMemberMutationData {
				Analyzed = analyzed.Contains(mutation),
				Discovered = discovered.Contains(mutation)
			};
		}

		return mutations;
	}
}
