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
                Collection = member.ExtractCropCollections(),
                Tools = member.Farming.ToMapOfCollectedItems()
            };
            // Initial run, no need to check for progress
            return;
        }
        
        var initialTools = eventMember.StartConditions.Tools;
        var currentTools = member.Farming.Inventory?.Tools ?? new List<ItemDto>();
        
        var initialCollections = eventMember.StartConditions.Collection;
        var currentCollections = member.ExtractCropCollections();
        
        var toolIncreases = new Dictionary<Crop, long>();
        var countedCollections = new Dictionary<Crop, long>();
        
        // Remove tools that are no longer in the inventory
        foreach (var item in initialTools) {
            if (currentTools.Any(tool => tool.Uuid == item.Key)) continue;
            
            // Tool is no longer in the inventory, remove it from the initial tools
            // This prevents trading tools to other players, then having them trade it back to you with more collection
            initialTools.Remove(item.Key);
        }
        
        // Add new tools to the initial tools
        foreach (var tool in currentTools.Where(tool => tool.Uuid is not null)) {
            if (!initialTools.ContainsKey(tool.Uuid!)) {
                initialTools.Add(tool.Uuid!, tool.Count);
                continue;
            }
            
            var crop = tool.ExtractCrop();
            if (crop is null) continue;
            
            var initialCount = initialTools[tool.Uuid!];
            var currentCount = tool.ExtractCollected();
            
            if (currentCount <= initialCount) continue;
            
            if (!toolIncreases.ContainsKey(crop.Value)) {
                toolIncreases.Add(crop.Value, currentCount - initialCount);
            }
            else {
                toolIncreases[crop.Value] += currentCount - initialCount;
            }
        }
        
        // Need to make sure that increased collection numbers match the increased tool numbers
        // This is to prevent other sources of collection increases from being counted

        foreach (var (crop, current) in currentCollections) {
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