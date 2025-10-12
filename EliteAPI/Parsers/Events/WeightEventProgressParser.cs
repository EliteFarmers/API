using EliteAPI.Models.Entities.Events;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Parsers.Farming;

namespace EliteAPI.Parsers.Events;

public static class WeightEventProgressParser {
	/// <summary>
	/// Cap mushrooms attributed to Mushroom Eater perk to 95% of 72,000 per hour
	/// </summary>
	private const int MaxMushroomEaterPerHour = (int)(72_000 * 0.95);

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

		var toolsByCrop = eventMember.Data.ToolStates
			.Where(t => t.Value.Crop.HasValue)
			.Select(t => t.Value)
			.GroupBy(t => t.Crop!.Value)
			.ToDictionary(g => g.Key, g => g.ToList());

		var mushroomEaterMushrooms = GetMushroomEaterMushrooms(eventMember, collectionIncreases, toolsByCrop);

		foreach (var (crop, tools) in toolsByCrop) {
			collectionIncreases.TryGetValue(crop, out var increasedCollection);
			if (increasedCollection <= 0) continue; // Skip if the collection didn't increase

			var counterIncrease = tools.Sum(t => t.Counter.IncreaseFromInitial());

			// Counter should always be prioritized over cultivating
			// This handles Wheat, Carrot, Potato, Sugar Cane, and Nether Wart
			if (counterIncrease > 0) {
				countedCollections.TryAdd(crop, Math.Min(counterIncrease, increasedCollection));
				continue;
			}

			// Cultivating should be used if the counter increase is 0
			// This handles Mushroom, Cocoa Beans, Cactus, Pumpkin, and Melon
			var cultivatingIncrease = tools.Sum(t => t.Cultivating.IncreaseFromInitial());
			if (cultivatingIncrease <= 0) continue; // Skip if the cultivating didn't increase

			// If there's a positive difference between the increased collection and cultivating
			// and the mushroom eater mushrooms are greater than 0, then remove the difference
			// from the counted collection to avoid counting extra mushrooms
			// (This is needed because cultivating increase includes these mushrooms)
			var difference = increasedCollection - cultivatingIncrease;
			if (difference > 0 && mushroomEaterMushrooms > 0 && crop != Crop.Mushroom) {
				var toRemove = Math.Min(difference, mushroomEaterMushrooms);

				cultivatingIncrease -= toRemove;
				mushroomEaterMushrooms -= toRemove;
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

	/// <summary>
	/// Get maximum mushrooms attributed to Mushroom Eater perk
	/// </summary>
	private static long GetMushroomEaterMushrooms(WeightEventMember eventMember,
		Dictionary<Crop, long> collectionIncreases,
		Dictionary<Crop, List<EventToolState>> toolsByCrop) {
		// Get unaccounted for mushroom collection for dealing with Mushroom Eater perk
		collectionIncreases.TryGetValue(Crop.Mushroom, out var increasedMushroom);

		var farmedMushroom = toolsByCrop.TryGetValue(Crop.Mushroom, out var mushroomTools)
			? mushroomTools.Sum(t => t.Cultivating.IncreaseFromInitial())
			: 0;

		var elapsedHours = eventMember.EstimatedTimeActive / 3600.0;
		var maxMushroomEater = (int)(elapsedHours * MaxMushroomEaterPerHour);
		var mushroomEaterMushrooms = Math.Min(Math.Max(increasedMushroom - farmedMushroom, 0), maxMushroomEater);

		return mushroomEaterMushrooms;
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