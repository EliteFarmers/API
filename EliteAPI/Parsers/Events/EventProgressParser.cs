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
                Tools = member.Farming.ToMapOfCollectedItems()
            };
            // Initial run, no need to check for progress
            return;
        }

        var initialTools = member.Farming.ToMapOfCollectedItems(eventMember.StartConditions.Tools);
        var currentTools = member.Farming.Inventory?.Tools ?? new List<ItemDto>();
        
        var initialCollections = eventMember.StartConditions.Collection;
        var currentCollections = member.ExtractCropCollections(true);
        
        var toolIncreases = new Dictionary<Crop, long> {
            { Crop.Mushroom, 0 }
        };
        var countedCollections = new Dictionary<Crop, long>();
        
        // Remove tools that are no longer in the inventory
        foreach (var item in initialTools) {
            if (currentTools.Any(tool => tool.Uuid == item.Key)) continue;
            
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

            initialTools.TryGetValue(tool.Uuid!, out var initialCount);
            var currentCount = tool.ExtractCollected();
            
            var increasedCount = currentCount - initialCount;
            if (increasedCount == 0) continue;
            
            if (!toolIncreases.ContainsKey(crop.Value)) {
                toolIncreases.Add(crop.Value, increasedCount);
            }
            else {
                toolIncreases[crop.Value] += increasedCount;
            }
            
            initialTools.TryGetValue($"{tool.Uuid!}-c", out var initialCultivating);
            var cultivatedCount = tool.ExtractCultivating();
            
            // Account for mushrooms being included on cultivating by checking if the cultivated count is higher than the collected count
            
            var increasedCultivating = cultivatedCount - initialCultivating - increasedCount;

            // Also subtract the amount of seeds collected from the cultivated count if the crop is wheat
            if (crop == Crop.Wheat) {
                increasedCultivating -= increasedSeeds;
            }
            
            // If the cultivated count is higher than the collected count, then the cultivated count is the amount of mushrooms collected
            if (increasedCultivating > 0) {
                toolIncreases[Crop.Mushroom] += increasedCultivating;
            }
        }
        
        // Need to make sure that increased collection numbers match the increased tool numbers
        // This is to prevent other sources of collection increases from being counted

        foreach (var (crop, current) in currentCollections) {
            if (crop == Crop.Seeds) continue;
            
            var initial = initialCollections.TryGetValue(crop, out var initialCollection) ? initialCollection : 0;
            var increase = toolIncreases.TryGetValue(crop, out var toolIncrease) ? toolIncrease : 0;
            
            if (current <= initial || increase == 0) continue;
            
            if (!countedCollections.ContainsKey(crop)) {
                countedCollections.Add(crop, increase);
            }
            else {
                countedCollections[crop] += increase;
            }
        }
        
        // Sum up the total farming weight increase
        var cropWeight = countedCollections.ParseCropWeight();
        eventMember.AmountGained = cropWeight.Sum(x => x.Value);
    }
}