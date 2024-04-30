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

public class MedalEventMember : EventMember {
	[Column("Data", TypeName = "jsonb")]
	public MedalEventMemberData Data { get; set; } = new();

	public MedalEventMember() {
		Type = EventType.Medals;
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