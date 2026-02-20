using EliteAPI.Data;
using EliteAPI.Features.Resources.Items.Models;
using EliteFarmers.HypixelAPI;
using EliteFarmers.HypixelAPI.DTOs;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using SkyblockRepo;

namespace EliteAPI.Features.Resources.Items.Services;

[RegisterService<SkyblockItemsIngestionService>(LifeTime.Scoped)]
public class SkyblockItemsIngestionService(
	IHypixelApi hypixelApi,
	DataContext context,
	ISkyblockRepoClient repo,
	ILogger<SkyblockItemsIngestionService> logger)
{
	public async Task IngestItemsDataAsync() {
		var apiResponse = await hypixelApi.FetchItemsAsync();

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
			var repoItem = repo.FindItem(item.Id);

			if (existingItems.TryGetValue(item.Id, out var skyblockItem)) {
				// Update existing record
				skyblockItem.NpcSellPrice = item.NpcSellPrice;
				skyblockItem.Data = item;
				skyblockItem.Data.Name = repoItem?.Name ?? repoItem?.Data?.Name ?? item.Name;
				updatedCount++;
			}
			else {
				// Insert new record
				var newItem = new SkyblockItem {
					ItemId = item.Id,
					NpcSellPrice = item.NpcSellPrice,
					Data = item
				};
				
				newItem.Data.Name = repoItem?.Data?.Name ?? item.Name;

				context.SkyblockItems.Add(newItem);
				newCount++;
			}
		}

		foreach (var item in existingItems.Values) {
			var repoItem = repo.FindItem(item.ItemId);
			var name = repoItem?.Name ?? repoItem?.Data?.Name ?? item.ItemId;
			
			if (item.Data is not null) {
				if (item.Data.Name == name) {
					continue;	
				}
				
				item.Data.Name = name;
				context.Entry(item).Property(x => x.Data).IsModified = true;
				
				updatedCount++;
				continue;
			}
			
			item.Data = new ItemResponse() {
				Id = item.ItemId,
				Name = repoItem?.Name ?? repoItem?.Data?.Name ?? item.ItemId,
			};
			
			updatedCount++;
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