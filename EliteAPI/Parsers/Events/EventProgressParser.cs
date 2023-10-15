using EliteAPI.Data;
using EliteAPI.Parsers.Farming;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Events;
using EliteAPI.Models.Entities.Hypixel;

namespace EliteAPI.Parsers.Events; 

public static class EventProgressParser {

    public static void LoadProgress(this EventMember eventMember, ProfileMember member) {
        var currentTime = DateTimeOffset.UtcNow;

        if (eventMember.Status == EventMemberStatus.Disqualified) return;

        if (eventMember.StartTime > currentTime || eventMember.EndTime < currentTime) {
            eventMember.Status = EventMemberStatus.Inactive;
            return;
        }
        
        eventMember.LastUpdated = currentTime;

        if (!member.Api.Collections || !member.Api.Inventories) {
            eventMember.Status = EventMemberStatus.Disqualified;
            eventMember.Notes = "API access was disabled during the event.";
            return;
        }

        if (eventMember.StartConditions.Collection.Count == 0) {
            eventMember.StartConditions = new StartConditions {
                Collection = member.ExtractCropCollections(true),
                Tools = member.Farming.ExtractToolCounters()
            };
            // Initial run, no need to check for progress
            return;
        }

        var initialTools = member.Farming.ExtractToolCounters(eventMember.StartConditions.Tools);
        var currentTools = member.Farming.Inventory?.Tools ?? new List<ItemDto>();
        
        // Remove tools that are no longer in the inventory to prevent collection increases from trading tools to other players
        initialTools.RemoveMissingTools(currentTools);
        
        // Save the initial tools again to account for new tools being added
        eventMember.StartConditions.Tools = initialTools;

        var toolIncreases = initialTools.ExtractIncreasedToolCollections(currentTools);
        
        var initialCollections = eventMember.StartConditions.Collection;
        var currentCollections = member.ExtractCropCollections(true);

        var collectionIncreases = initialCollections.ExtractCollectionIncreases(currentCollections);
        // Get farmed mushroom from the tools for counting Mushroom Eater perk
        collectionIncreases.TryGetValue(Crop.Mushroom, out var increasedMushroom);

        var mushroomTools = currentTools.FindAll(x => x.ExtractCrop() == Crop.Mushroom && initialTools.ContainsKey(x.Uuid!));
        var farmedMushroom = mushroomTools.Sum(x => x.ExtractCultivating());
        
        var mushroomEaterMushrooms = Math.Max(increasedMushroom - farmedMushroom, 0);

        var cropIncreases = new Dictionary<Crop, long> {
            { Crop.Mushroom, mushroomEaterMushrooms }
        };
        var countedCollections = new Dictionary<Crop, long>();
        
        // Remove tools that are no longer in the inventory
        foreach (var item in initialTools) {
            if (currentTools.Any(tool => tool.Uuid == item.Key.Replace("-c", ""))) continue;
            
            // Tool is no longer in the inventory, remove it from the initial tools
            // This prevents trading tools to other players, then having them trade it back to you with more collection
            initialTools.Remove(item.Key);
            initialTools.Remove($"{item.Key}-c");
        }

        // Check if the player has collected any seeds for wheat
        var currentSeeds = currentCollections.TryGetValue(Crop.Seeds, out var currentSeedsCount) ? currentSeedsCount : 0;
        var initialSeeds = initialCollections.TryGetValue(Crop.Seeds, out var initialSeedCount) ? initialSeedCount : 0;
        var increasedSeeds = currentSeeds - initialSeeds;
        
        foreach (var tool in currentTools.Where(tool => tool.Uuid is not null)) {
            var crop = tool.ExtractCrop();
            if (crop is null) continue;

            collectionIncreases.TryGetValue(crop.Value, out var increasedCollection);
            if (increasedCollection <= 0) continue; // Skip if the collection didn't increase
            
            toolIncreases.TryGetValue(tool.Uuid!, out var counterIncrease);
            toolIncreases.TryGetValue($"{tool.Uuid!}-c", out var cultivatingIncrease);
            
            if (counterIncrease <= 0 && cultivatingIncrease <= 0) {
                cropIncreases.Add(crop.Value, 0);
                continue;
            }
            
            if (counterIncrease > 0) {
                cropIncreases.Add(crop.Value, Math.Min(counterIncrease, increasedCollection));
                continue;
            }

            // Use cultivating if the counter increase is 0
            if (crop == Crop.Wheat) {
                // Subtract the amount of seeds collected from the cultivated count if the crop is wheat
                cultivatingIncrease -= increasedSeeds;
            }

            //cultivatingIncrease -= increasedCollection;
            var remaining = increasedCollection - cultivatingIncrease;
            
            if (remaining > 0 && mushroomEaterMushrooms > 0 && crop != Crop.Mushroom) {
                var toRemove = Math.Min(remaining, mushroomEaterMushrooms);
                
                cultivatingIncrease -= toRemove;
                mushroomEaterMushrooms -= toRemove;
            }
            
            cropIncreases.Add(crop.Value, Math.Min(increasedCollection, cultivatingIncrease));
        
            
            initialTools.TryGetValue($"{tool.Uuid!}-c", out var initialCultivating);
            var cultivatedCount = tool.ExtractCultivating();
            
            if (cultivatedCount <= initialCultivating) continue;
            
            // Account for mushrooms being included on cultivating by checking if the cultivated count is higher than the collected count

            var increasedCultivating = cultivatedCount - initialCultivating;//- increasedCount;

            // Also subtract the amount of seeds collected from the cultivated count if the crop is wheat
            if (crop == Crop.Wheat) {
                increasedCultivating -= increasedSeeds;
            }
            
            // If the cultivated count is higher than the collected count, then the cultivated count is the amount of mushrooms collected
            if (increasedCultivating > 0) {
                cropIncreases[Crop.Mushroom] += increasedCultivating;
            }
        }
        
        // Need to make sure that increased collection numbers match the increased tool numbers
        // This is to prevent other sources of collection increases from being counted

        foreach (var (crop, currentCollection) in currentCollections) {
            if (crop == Crop.Seeds) continue;
            
            if (!initialCollections.TryGetValue(crop, out var initialCollection)) continue;
            if (!cropIncreases.TryGetValue(crop, out var toolIncrease)) continue;
            
            var increasedCollection = currentCollection - initialCollection;
            if (increasedCollection == 0 || toolIncrease == 0) continue;

            var counted = Math.Min(increasedCollection, toolIncrease);
            
            if (!countedCollections.ContainsKey(crop)) {
                countedCollections.Add(crop, counted);
            }
            else {
                countedCollections[crop] += counted;
            }
        }
        
        // Sum up the total farming weight increase
        var cropWeight = countedCollections.ParseCropWeight(true);
        eventMember.AmountGained = cropWeight.Sum(x => x.Value);
        
        // Update the start conditions with the new/removed tools
        eventMember.StartConditions = new StartConditions {
            Collection = eventMember.StartConditions.Collection,
            Tools = initialTools
        };
    }
}