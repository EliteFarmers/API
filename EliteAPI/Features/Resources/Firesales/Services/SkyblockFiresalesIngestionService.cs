using EliteAPI.Data;
using EliteAPI.Features.Resources.Firesales.Models;
using FastEndpoints;
using HypixelAPI;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Resources.Firesales.Services;

[RegisterService<SkyblockFiresalesIngestionService>(LifeTime.Scoped)]
public class SkyblockFiresalesIngestionService(
    IHypixelApi hypixelApi,
    DataContext context,
    ILogger<SkyblockFiresalesIngestionService> logger) 
{
    public async Task IngestItemsDataAsync() {
        var apiResponse = await hypixelApi.FetchFiresales();
        
        if (!apiResponse.IsSuccessStatusCode || apiResponse.Content is not { Success: true }) {
            var errorContent = apiResponse.Error != null
                ? apiResponse.Error.ToString()
                : "Unknown error";
            logger.LogError("Failed to fetch skyblock firesales data. Status: {StatusCode}. Error: {Error}",
                apiResponse.StatusCode, errorContent);
            return;
        }
        
        var newCount = 0;
        
        var sales = apiResponse.Content.Sales.GroupBy(s => s.Start);
        foreach (var group in sales)
        {
            var start = DateTimeOffset.FromUnixTimeMilliseconds(group.Key).ToUnixTimeSeconds();
            var items = group.ToList();
            if (items.Count == 0) continue;
            
            var existingSale = await context.SkyblockFiresales
                .Include(s => s.Items)
                .FirstOrDefaultAsync(s => s.StartsAt == start);

            if (existingSale is null) {
                var newSale = new SkyblockFiresale
                {
                    StartsAt = start,
                    EndsAt = DateTimeOffset.FromUnixTimeMilliseconds(items.First().End).ToUnixTimeSeconds(),
                    Items = items.Select(i => new SkyblockFiresaleItem
                    {
                        ItemId = i.ItemId,
                        Amount = i.Amount,
                        Price = i.Price,
                        StartsAt = DateTimeOffset.FromUnixTimeMilliseconds(i.Start).ToUnixTimeSeconds(),
                        EndsAt = DateTimeOffset.FromUnixTimeMilliseconds(i.End).ToUnixTimeSeconds()
                    }).ToList()
                };
                newCount++;
                
                context.SkyblockFiresales.Add(newSale);
                continue;
            }
            
            foreach (var item in items) {
                var existingItem = existingSale.Items.FirstOrDefault(i => i.ItemId == item.ItemId);
                if (existingItem is not null)
                {
                    existingItem.Amount = item.Amount;
                    existingItem.Price = item.Price;
                    existingItem.EndsAt = DateTimeOffset.FromUnixTimeMilliseconds(item.End).ToUnixTimeSeconds();
                    existingItem.StartsAt = DateTimeOffset.FromUnixTimeMilliseconds(item.Start).ToUnixTimeSeconds();
                    existingItem.ItemId = item.ItemId;
                    continue;
                }
                    
                existingSale.Items.Add(new SkyblockFiresaleItem
                {
                    ItemId = item.ItemId,
                    Amount = item.Amount,
                    Price = item.Price,
                    StartsAt = DateTimeOffset.FromUnixTimeMilliseconds(item.Start).ToUnixTimeSeconds(),
                    EndsAt = DateTimeOffset.FromUnixTimeMilliseconds(item.End).ToUnixTimeSeconds()
                });
            }
        }
        
        await context.SaveChangesAsync();
        
        if (newCount > 0) {
            await context.SaveChangesAsync();
            logger.LogInformation(
                "Updated Firesales: {NewCount} new", newCount);
        }
        else {
            logger.LogInformation("No Firesales found");
        }
    }
}