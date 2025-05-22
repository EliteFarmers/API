using EliteAPI.Data;
using EliteAPI.Features.Resources.Items.Models;
using FastEndpoints;
using HypixelAPI;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Resources.Items.Services;

[RegisterService<SkyblockItemsIngestionService>(LifeTime.Scoped)]
public class SkyblockItemsIngestionService(
    IHypixelApi hypixelApi,
    DataContext context,
    ILogger<SkyblockItemsIngestionService> logger) 
{
    public async Task IngestItemsDataAsync() {
        var apiResponse = await hypixelApi.FetchItems();
        
        if (!apiResponse.IsSuccessStatusCode || apiResponse.Content is not { Success: true }) {
            var errorContent = apiResponse.Error != null
                ? apiResponse.Error.ToString()
                : "Unknown error";
            logger.LogError("Failed to fetch skyblock item data. Status: {StatusCode}. Error: {Error}",
                apiResponse.StatusCode, errorContent);
            return;
        }

        var items = apiResponse.Content.Items;

        var existingItems = await context.SkyblockItems
            .ToDictionaryAsync(p => p.ItemId);

        var newCount = 0;
        var updatedCount = 0;

        foreach (var item in items) {
            if (item.Id is null) continue;
            
            if (existingItems.TryGetValue(item.Id, out var skyblockItem)) {
                // Update existing record
                skyblockItem.NpcSellPrice = item.NpcSellPrice;
                skyblockItem.Data = item;
                updatedCount++;
            }
            else {
                // Insert new record
                var newItem = new SkyblockItem {
                    ItemId = item.Id,
                    NpcSellPrice = item.NpcSellPrice,
                    Data = item,
                };
                
                context.SkyblockItems.Add(newItem);
                newCount++;
            }
        }

        if (newCount > 0 || updatedCount > 0) {
            await context.SaveChangesAsync();
            logger.LogInformation(
                "Updated Skyblock items: {NewCount} new, {UpdatedCount} updated", newCount, updatedCount);
        }
        else {
            logger.LogInformation("No updated Skyblock items");
        }
    }
}