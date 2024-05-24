using System.ComponentModel.DataAnnotations.Schema;
using EliteAPI.Models.Entities.Hypixel;

namespace EliteAPI.Models.Entities.Events;

public class MedalEvent : Event {
	[Column("Data", TypeName = "jsonb")]
	public MedalEventData Data { get; set; } = new();

	public MedalEvent() {
		Type = EventType.Medals;
	}
}

public class MedalEventMember : EventMember, IComparable<MedalEventMember> {
	[Column("Data", TypeName = "jsonb")]
	public MedalEventMemberData Data { get; set; } = new();

	public MedalEventMember() {
		Type = EventType.Medals;
	}

	public int CompareTo(MedalEventMember? other) {
		if (other == null) return 1;
		
		if (!Score.Equals(other.Score)) {
			return Score.CompareTo(other.Score);
		}

		// Compare earned medals
		var medals = Data.EarnedMedals;
		var otherMedals = other.Data.EarnedMedals;
		
		var difference = 0;
		if (CompareMedals(ContestMedal.Diamond)) return difference;
		if (CompareMedals(ContestMedal.Platinum)) return difference;
		if (CompareMedals(ContestMedal.Gold)) return difference;
		if (CompareMedals(ContestMedal.Silver)) return difference;
		if (CompareMedals(ContestMedal.Bronze)) return difference;
		
		return Data.ContestParticipations.CompareTo(other.Data.ContestParticipations);
		
		bool CompareMedals(ContestMedal medal) {
			var count = medals.GetValueOrDefault(medal);
			var otherCount = otherMedals.GetValueOrDefault(medal);
			difference = count.CompareTo(otherCount);
			
			return difference != 0;
		}
	}
}

public class MedalEventData {
	public Dictionary<ContestMedal, int> MedalWeights { get; set; } = new() {
		{ ContestMedal.Bronze, 1 },
		{ ContestMedal.Silver, 2 },
		{ ContestMedal.Gold, 5 },
		{ ContestMedal.Platinum, 7 },
		{ ContestMedal.Diamond, 10 }
	};
}

public class MedalEventMemberData {
	public int ContestParticipations { get; set; }
	public Dictionary<ContestMedal, int> EarnedMedals { get; set; } = new();
}