using EliteAPI.Configuration.Settings;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Events;
using EliteAPI.Models.Entities.Hypixel;
using Microsoft.IdentityModel.Tokens;

namespace EliteAPI.Parsers.Farming; 

public static class FarmingToolParser {
    
    public static Dictionary<string, EventToolState> ExtractToolStates(this List<ItemDto> items, Dictionary<string, EventToolState> existing) {
        var newStates = items.ExtractToolStates();
        
        foreach (var (uuid, existingState) in existing) {
            existingState.LastSeen = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            if (!newStates.TryGetValue(uuid, out var newState)) {
                // Tool was in inventory, now is not
                existingState.IsActive = false;
                
                // Add the tool without updating the current values
                newStates.Add(uuid, existingState);
                continue;
            }
            
            // Tool was removed from inventory, then added back
            if (!existingState.IsActive) {
                // Add the counts gained while the tool was inactive to the uncounted values
                existingState.Counter.Uncounted += newState.Counter.Current - existingState.Counter.Current;
                existingState.Cultivating.Uncounted += newState.Cultivating.Current - existingState.Cultivating.Current;
            }
                
            existingState.IsActive = true;
            existingState.LastSeen = newState.LastSeen;
            
            // Update the previous values
            existingState.Counter.Previous = existingState.Counter.Current;
            existingState.Cultivating.Previous = existingState.Cultivating.Current;
            
            // Update the current values
            existingState.Counter.Current = newState.Counter.Current;
            existingState.Cultivating.Current = newState.Cultivating.Current;

            newStates[uuid] = existingState;
        }
        
        return newStates;
    }
    
    public static Dictionary<string, EventToolState> ExtractToolStates(this List<ItemDto> items) {
        var toolStates = new Dictionary<string, EventToolState>();
        var time = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        foreach (var item in items) {
            var uuid = item.Uuid;
            
            if (uuid is null || item.SkyblockId is null || uuid.IsNullOrEmpty()) continue;

            var counter = item.ExtractCounter();
            var cultivating = item.ExtractCultivating();
            
            var state = new EventToolState {
                SkyblockId = item.SkyblockId,
                Crop = item.ExtractCrop(),
                FirstSeen = time,
                LastSeen = time,
                IsActive = true,
                Counter = new EventToolCounterState(counter),
                Cultivating = new EventToolCounterState(cultivating)
            };
            
            toolStates.Add(uuid, state);
        }
        
        return toolStates;
    }
    
    public static Dictionary<string, long> ExtractToolCounters(this Models.Entities.Farming.Farming farming, Dictionary<string, long>? existing = null) {
        return farming.Inventory?.Tools.ExtractToolCounters(existing) ?? new Dictionary<string, long>();
    }

    public static Dictionary<string, long> ExtractToolCounters(this List<ItemDto> items,
        Dictionary<string, long>? existing = null) {
        var tools = existing ?? new Dictionary<string, long>();

        if (items is null or { Count: 0 }) return tools;
        
        foreach (var item in items)
        {
            var uuid = item.Uuid;
            if (uuid is null || uuid.IsNullOrEmpty()) continue;
            
            // Skip if the item is already in the dictionary
            if (tools.ContainsKey(uuid)) continue;
            
            // Add initial tools to the dictionary
            tools.Add(uuid, item.ExtractCounter());
            tools.Add($"{uuid}-c", item.ExtractCultivating());
        }
        
        return tools;
    }
    
    public static void RemoveMissingTools(this Dictionary<string, long> tools, List<ItemDto> currentTools) {
        if (currentTools is null or { Count: 0 }) return;
        
        // Simple way to get the uuids from the tools
        var currentToolCounters = currentTools.ExtractToolCounters();

        // This will work for both counters and cultivating entries
        foreach (var key in tools.Keys) {
            if (currentToolCounters.ContainsKey(key)) continue;

            // Tool is no longer in the inventory, remove it from the initial tools
            // This prevents trading tools to other players, then having them trade it back to you with more collection
            tools.Remove(key);
        }
    }
    
    public static Dictionary<string, long> ExtractIncreasedToolCollections(this Dictionary<string, long> initialTools, List<ItemDto> currentTools) {
        return initialTools.ExtractIncreasedToolCollections(currentTools.ExtractToolCounters());
    }
    
    public static Dictionary<string, long> ExtractIncreasedToolCollections(this Dictionary<string, long> initialTools, Dictionary<string, long> currentTools) {
        var toolIncreases = new Dictionary<string, long>();
        if (currentTools is null or { Count: 0 }) return toolIncreases;
        
        // This will work for both counters and cultivating entries
        foreach (var key in initialTools.Keys) {
            if (!currentTools.ContainsKey(key)) continue;
            
            var initialCount = initialTools[key];
            var currentCount = currentTools[key];
            
            var increase = Math.Max(currentCount - initialCount, 0);
            
            toolIncreases.Add(key, increase);
        }

        return toolIncreases;
    }

    public static Crop? ExtractCrop(this ItemDto tool) {
        var toolIds = FarmingItemsConfig.Settings.FarmingToolIds;
        
        if (tool.SkyblockId is null) return null;
        if (!toolIds.TryGetValue(tool.SkyblockId, out var crop)) return null;
        
        return crop;
    }
    
    public static long ExtractCollected(this ItemDto tool) {
        if (tool.Attributes?.TryGetValue("mined_crops", out var collected) is true 
            && long.TryParse(collected, out var mined)) {
            return mined;
        }
            
        if (tool.Attributes?.TryGetValue("farmed_cultivating", out var cultivated) is true 
            && long.TryParse(cultivated, out var crops)) {
            return crops;
        }
        
        return 0;
    }
    
    public static long ExtractCounter(this ItemDto tool) {
        if (tool.Attributes?.TryGetValue("mined_crops", out var collected) is true 
            && long.TryParse(collected, out var mined)) {
            return mined;
        }

        return 0;
    }
    
    public static long ExtractCultivating(this ItemDto tool) {
        if (tool.Attributes?.TryGetValue("farmed_cultivating", out var cultivated) is true 
            && long.TryParse(cultivated, out var crops)) {
            return crops;
        }
        
        return 0;
    }
}