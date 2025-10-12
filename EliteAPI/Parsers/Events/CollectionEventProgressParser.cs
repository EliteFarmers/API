using EliteAPI.Models.Entities.Events;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Parsers.Profiles;

namespace EliteAPI.Parsers.Events;

public static class CollectionEventProgressParser {
	public static void UpdateScore(this CollectionEventMember eventMember, CollectionEvent @event,
		ProfileMember? member = null) {
		if (member is not null) {
			// Initialize the start conditions if they haven't been initialized yet
			if (eventMember.Data.InitialCollections.Count == 0) {
				eventMember.Initialize(@event, member);
				// Initial run, no need to check for progress
				return;
			}
		}
		else {
			return;
		}

		var initial = eventMember.Data.InitialCollections;
		var counted = new Dictionary<string, long>();

		foreach (var key in @event.Data.CollectionWeights.Keys) {
			if (!member.TryGetCollection(key, out var amount)) continue;
			if (!initial.TryGetValue(key, out var startingAmount)) {
				initial.Add(key, amount);
				startingAmount = amount;
			}

			counted.TryAdd(key, amount - startingAmount);
		}

		var score = counted.Aggregate(0.0, (acc, collection) => {
			if (!@event.Data.CollectionWeights.TryGetValue(collection.Key, out var weight)) return acc;

			return acc + collection.Value / weight.Weight;
		});

		// Update the event member and amount gained
		eventMember.Data.CountedCollections = counted;
		eventMember.Status = score > eventMember.Score ? EventMemberStatus.Active : EventMemberStatus.Inactive;
		eventMember.Score = score;
	}

	public static void Initialize(this CollectionEventMember eventMember, CollectionEvent @event,
		ProfileMember member) {
		if (eventMember.Data.InitialCollections.Count > 0) return;

		var collections = new Dictionary<string, long>();

		foreach (var key in @event.Data.CollectionWeights.Keys) {
			collections.TryAdd(key, member.GetCollection(key));
		}

		eventMember.Data = new CollectionEventMemberData {
			InitialCollections = collections
		};
	}
}