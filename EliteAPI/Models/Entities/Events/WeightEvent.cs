using System.ComponentModel.DataAnnotations.Schema;
using EliteAPI.Models.Entities.Hypixel;

namespace EliteAPI.Models.Entities.Events;

public class WeightEvent : Event {
	[Column("Data", TypeName = "jsonb")] public WeightEventData Data { get; set; } = new();

	public WeightEvent() {
		Type = EventType.FarmingWeight;
	}
}

public class WeightEventMember : EventMember {
	[Column("Data", TypeName = "jsonb")] public EventMemberWeightData Data { get; set; } = new();

	public WeightEventMember() {
		Type = EventType.FarmingWeight;
	}
}

public class WeightEventData {
	/// <summary>
	/// The weights of each crop in the event
	/// </summary>
	public Dictionary<Crop, double> CropWeights { get; set; } = new();
}

public class EventToolState {
	public required string SkyblockId { get; set; }
	public Crop? Crop { get; set; }

	public long FirstSeen { get; set; }
	public long LastSeen { get; set; }
	public bool IsActive { get; set; }

	public EventToolCounterState Counter { get; set; } = new();
	public EventToolCounterState Cultivating { get; set; } = new();
}

public class EventMemberWeightData {
	public Dictionary<Crop, long> InitialCollection { get; set; } = new();
	public Dictionary<Crop, long> IncreasedCollection { get; set; } = new();
	public Dictionary<Crop, long> CountedCollection { get; set; } = new();
	public Dictionary<string, EventToolState> ToolStates { get; set; } = new();
	public Dictionary<string, long> Tools { get; set; } = new();
}

public class EventToolCounterState {
	public long Initial { get; set; }
	public long Previous { get; set; }
	public long Current { get; set; }
	public long Uncounted { get; set; }

	public EventToolCounterState() {
	}

	public EventToolCounterState(long initial) {
		Initial = initial;
		Previous = initial;
		Current = initial;
	}
}

public static class ToolCounterStateExtensions {
	public static long IncreaseFromInitial(this EventToolCounterState e) {
		// Counter can overflow into negatives
		if (e is { Initial: > 0, Current: < 0 }) {
			var initial = e.Initial - int.MaxValue - int.MaxValue;
			return e.Current - initial - e.Uncounted;
		}

		return e.Current - e.Initial - e.Uncounted;
	}

	public static long IncreaseFromPrevious(this EventToolCounterState e) {
		return e.Current - e.Previous - e.Uncounted;
	}
}