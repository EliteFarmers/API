using System.ComponentModel.DataAnnotations.Schema;
using EliteAPI.Models.Entities.Hypixel;

namespace EliteAPI.Models.Entities.Events;

public class PestEvent : Event {
	[Column("Data", TypeName = "jsonb")]
	public PestEventData Data { get; set; } = new();

	public PestEvent() {
		Type = EventType.Pests;
	}
}

public class PestEventMember : EventMember {
	[Column("Data", TypeName = "jsonb")]
	public PestEventMemberData Data { get; set; } = new();

	public PestEventMember() {
		Type = EventType.Pests;
	}
}

public class PestEventData {
	public Dictionary<Pest, int> PestWeights { get; set; } = new() {
		{ Pest.Mite, 1 },
		{ Pest.Cricket, 1 },
		{ Pest.Moth, 1 },
		{ Pest.Earthworm, 1 },
		{ Pest.Slug, 1 },
		{ Pest.Beetle, 1 },
		{ Pest.Locust, 1 },
		{ Pest.Rat, 1 },
		{ Pest.Mosquito, 1 },
		{ Pest.Fly, 1 },
		{ Pest.Mouse, 10 },
	};
}

public class PestEventMemberData {
	public Dictionary<Pest, int> InitialPests { get; set; } = new();
	public Dictionary<Pest, int> CountedPests { get; set; } = new();
}