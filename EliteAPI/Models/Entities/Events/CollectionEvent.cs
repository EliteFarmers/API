using System.ComponentModel.DataAnnotations.Schema;

namespace EliteAPI.Models.Entities.Events;

public class CollectionEvent : Event
{
	[Column("Data", TypeName = "jsonb")] public CollectionEventData Data { get; set; } = new();

	public CollectionEvent() {
		Type = EventType.Collection;
	}
}

public class CollectionEventMember : EventMember
{
	[Column("Data", TypeName = "jsonb")] public CollectionEventMemberData Data { get; set; } = new();

	public CollectionEventMember() {
		Type = EventType.Collection;
	}
}

public class CollectionEventData
{
	public Dictionary<string, CollectionWeight> CollectionWeights { get; set; } = new();
}

public class CollectionWeight
{
	public string? Name { get; set; }
	public double Weight { get; set; }
}

public class CollectionEventMemberData
{
	public Dictionary<string, long> InitialCollections { get; set; } = new();
	public Dictionary<string, long> CountedCollections { get; set; } = new();
}