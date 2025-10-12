using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Parsers.Farming;
using EliteFarmers.HypixelAPI.DTOs;

namespace EliteAPI.Parsers.Profiles;

public static class JacobDataParser {
	public static void ParseJacob(this ProfileMember member, RawJacobData? incomingJacob) {
		member.JacobData = ParseJacobData(member.JacobData, incomingJacob);
		member.JacobData.ProfileMember = member;
		member.JacobData.ProfileMemberId = member.Id;
	}

	public static JacobData ParseJacobData(JacobData jacob, RawJacobData? incomingJacob) {
		if (incomingJacob is null) return jacob;

		if (incomingJacob.MedalsInventory is not null) {
			jacob.Medals.Gold = incomingJacob.MedalsInventory.Gold;
			jacob.Medals.Silver = incomingJacob.MedalsInventory.Silver;
			jacob.Medals.Bronze = incomingJacob.MedalsInventory.Bronze;
		}

		if (incomingJacob.Perks is not null) {
			jacob.Perks.DoubleDrops = incomingJacob.Perks.DoubleDrops ?? 0;
			jacob.Perks.LevelCap = incomingJacob.Perks.FarmingLevelCap ?? 0;
			jacob.Perks.PersonalBests = incomingJacob.Perks.PersonalBests;
		}

		jacob.Stats = new JacobStats();
		if (incomingJacob.UniqueBrackets is not null) {
			jacob.Stats.Brackets.PopulateBrackets(incomingJacob.UniqueBrackets.Diamond, ContestMedal.Diamond);
			jacob.Stats.Brackets.PopulateBrackets(incomingJacob.UniqueBrackets.Platinum, ContestMedal.Platinum);
			jacob.Stats.Brackets.PopulateBrackets(incomingJacob.UniqueBrackets.Gold, ContestMedal.Gold);
			jacob.Stats.Brackets.PopulateBrackets(incomingJacob.UniqueBrackets.Silver, ContestMedal.Silver);
			jacob.Stats.Brackets.PopulateBrackets(incomingJacob.UniqueBrackets.Bronze, ContestMedal.Bronze);
		}

		foreach (var (cropId, value) in incomingJacob.PersonalBests) {
			if (!cropId.TryGetCrop(out var key)) continue;

			jacob.Stats.PersonalBests[key] = value;
		}

		return jacob;
	}

	private static void PopulateBrackets(this Dictionary<Crop, ContestMedal> result, List<string> crops,
		ContestMedal medal) {
		foreach (var crop in crops) {
			if (!crop.TryGetCrop(out var key)) continue;

			if (result.TryGetValue(key, out var currentMedal)) {
				if (currentMedal < medal) result[key] = medal;
				continue;
			}

			result[key] = medal;
		}
	}
}