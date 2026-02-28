using EliteAPI.Models.Entities.Events;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Parsers.Farming;

namespace EliteAPI.Parsers.Events;

public static class WeightEventProgressParser
{
	public static void UpdateFarmingWeight(this WeightEventMember eventMember, WeightEvent weightEvent,
		ProfileMember? member = null) {
		if (member is not null) {
			eventMember.UpdateToolsAndCollections(member);

			// Initialize the start conditions if they haven't been initialized yet
			if (eventMember.Data.InitialCollection.Count == 0) {
				eventMember.Initialize(member);
				// Initial run, no need to check for progress
				return;
			}
		}

		var collectionIncreases = eventMember.Data.IncreasedCollection;
		var countedCollections = new Dictionary<Crop, long>();
		var seedCollectionIncrease = collectionIncreases.GetValueOrDefault(Crop.Seeds);

		var toolsByCrop = eventMember.Data.ToolStates
			.Where(t => t.Value.Crop.HasValue)
			.Select(t => t.Value)
			.GroupBy(t => t.Crop!.Value)
			.ToDictionary(g => g.Key, g => g.ToList());

		foreach (var (crop, tools) in toolsByCrop) {
			collectionIncreases.TryGetValue(crop, out var increasedCollection);
			if (increasedCollection <= 0) continue; // Skip if the collection didn't increase
			
			var cultivatingIncrease = tools.Sum(t => t.Cultivating.IncreaseFromInitial());
			if (cultivatingIncrease <= 0) continue; // Skip if the cultivating didn't increase
			
			// Wheat cultivating counter also increases by seed collection, so we need to account for that to avoid counting extra wheat
			// Subtract as much of the seed collection increase as possible from the cultivating increase
			if (crop == Crop.Wheat) {
				var toRemove = Math.Min(seedCollectionIncrease, cultivatingIncrease);
				cultivatingIncrease -= toRemove;
				seedCollectionIncrease -= toRemove;
			}

			countedCollections.TryAdd(crop, Math.Min(increasedCollection, cultivatingIncrease));
		}

		eventMember.Data.CountedCollection = countedCollections;

		var cropWeight = countedCollections.ParseCropWeight(weightEvent.Data.CropWeights, true);
		var newAmount = cropWeight.Sum(x => x.Value);

		// Update the event member status and amount gained
		eventMember.Status = newAmount > eventMember.Score ? EventMemberStatus.Active : EventMemberStatus.Inactive;
		eventMember.Score = newAmount;
	}
	
	public static void UpdateToolsAndCollections(this WeightEventMember eventMember, ProfileMember member) {
		var toolStates =
			member.Farming.Inventory?.Tools.ExtractToolStates(eventMember.Data.ToolStates)
			?? new Dictionary<string, EventToolState>();

		// Update the tool states
		eventMember.Data.ToolStates = toolStates;

		// Get collection increases
		var initialCollections = eventMember.Data.InitialCollection;
		var currentCollections = member.ExtractCropCollections(true);

		var collectionIncreases = initialCollections.ExtractCollectionIncreases(currentCollections);

		// Update the collection increases
		eventMember.Data.IncreasedCollection = collectionIncreases;
	}

	public static void Initialize(this WeightEventMember eventMember, ProfileMember member) {
		if (eventMember.Data.InitialCollection.Count > 0) return;

		eventMember.Data = new EventMemberWeightData {
			InitialCollection = member.ExtractCropCollections(true),
			ToolStates = member.Farming.Inventory?.Tools.ExtractToolStates() ?? new Dictionary<string, EventToolState>()
		};
	}
}