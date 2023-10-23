using EliteAPI.Parsers.Farming;
using EliteAPI.Models.Entities.Events;
using EliteAPI.Models.Entities.Hypixel;

namespace EliteAPI.Parsers.Events; 

public static class EventProgressParser {
    
    public static bool IsEventRunning(this EventMember member) {
        var currentTime = DateTimeOffset.UtcNow;
        
        // Check if event is running
        return member.StartTime > currentTime || member.EndTime < currentTime;
    }
    
    public static void LoadProgress(this EventMember eventMember, ProfileMember member, Event @event) {
        var currentTime = DateTimeOffset.UtcNow;

        // Skip if the member is already disqualified or the event hasn't started yet
        if (eventMember.Status == EventMemberStatus.Disqualified) return;
        if (!eventMember.IsEventRunning()) {
            eventMember.Status = EventMemberStatus.Inactive;
            return;
        }
        
        eventMember.LastUpdated = currentTime;

        // Disqualify the member if they disabled API access during the event
        if (!member.Api.Collections || !member.Api.Inventories) {
            eventMember.Status = EventMemberStatus.Disqualified;
            eventMember.Notes = "API access was disabled during the event.";
            return;
        }

        // Initialize the start conditions if they haven't been initialized yet
        if (eventMember.EventMemberStartConditions.InitialCollection.Count == 0) {
            eventMember.Initialize(member);
            // Initial run, no need to check for progress
            return;
        }
        
        // Update the tool states and collection increases
        eventMember.UpdateToolsAndCollections(member);

        switch (@event.Category) {
            case EventType.FarmingWeight:
                eventMember.UpdateFarmingWeight();
                break;
            case EventType.Collection:
                break;
            case EventType.Experience:
                break;
        }
    }
    
    public static void UpdateFarmingWeight(this EventMember eventMember) {
        var collectionIncreases = eventMember.EventMemberStartConditions.IncreasedCollection;
        var countedCollections = new Dictionary<Crop, long>();
        
        var toolsByCrop = eventMember.EventMemberStartConditions.ToolStates
            .Where(t => t.Value.Crop.HasValue)
            .Select(t => t.Value)
            .GroupBy(t => t.Crop!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Get unaccounted for mushroom collection for dealing with Mushroom Eater perk
        collectionIncreases.TryGetValue(Crop.Mushroom, out var increasedMushroom);
        
        var farmedMushroom = toolsByCrop.TryGetValue(Crop.Mushroom, out var mushroomTools)
            ? mushroomTools.Sum(t => t.Cultivating.IncreaseFromInitial()) : 0;
        var mushroomEaterMushrooms = Math.Max(increasedMushroom - farmedMushroom, 0);
        
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
            var difference = increasedCollection - cultivatingIncrease;
            if (difference > 0 && mushroomEaterMushrooms > 0 && crop != Crop.Mushroom) {
                var toRemove = Math.Min(difference, mushroomEaterMushrooms);
                
                cultivatingIncrease -= toRemove;
                mushroomEaterMushrooms -= toRemove;
            }
            
            countedCollections.TryAdd(crop, Math.Min(increasedCollection, cultivatingIncrease));
        }
        
        eventMember.EventMemberStartConditions.CountedCollection = countedCollections;
        
        var cropWeight = countedCollections.ParseCropWeight(true);
        eventMember.AmountGained = cropWeight.Sum(x => x.Value);
    }


    public static void UpdateToolsAndCollections(this EventMember eventMember, ProfileMember member) {
        var toolStates =
            member.Farming.Inventory?.Tools.ExtractToolStates(eventMember.EventMemberStartConditions.ToolStates)
            ?? new Dictionary<string, EventToolState>();

        // Update the tool states
        eventMember.EventMemberStartConditions.ToolStates = toolStates;
        
        // Get collection increases
        var initialCollections = eventMember.EventMemberStartConditions.InitialCollection;
        var currentCollections = member.ExtractCropCollections(true);

        var collectionIncreases = initialCollections.ExtractCollectionIncreases(currentCollections);
        
        // Update the collection increases
        eventMember.EventMemberStartConditions.IncreasedCollection = collectionIncreases;
    }

    public static void Initialize(this EventMember eventMember, ProfileMember member) {
        eventMember.EventMemberStartConditions = new EventMemberStartConditions {
            InitialCollection = member.ExtractCropCollections(true),
            ToolStates = member.Farming.Inventory?.Tools.ExtractToolStates() ?? new Dictionary<string, EventToolState>()
        };
    }
}