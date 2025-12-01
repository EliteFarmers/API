using System.Text.Json;
using EliteAPI.Data;
using EliteAPI.Features.Profiles.Mappers;
using EliteAPI.Features.Profiles.Models;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Parsers.Inventories;
using EliteAPI.Services.Interfaces;
using EliteAPI.Utilities;
using EliteFarmers.HypixelAPI.DTOs;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Profiles.Services;

public interface IMuseumService
{
	Task UpdateMuseum(string profileId, CancellationToken ct);
}

[RegisterService<IMuseumService>(LifeTime.Scoped)]
public class MuseumService(
	IHypixelService hypixelService,
	DataContext context
) : IMuseumService
{
	public async Task UpdateMuseum(string profileId, CancellationToken ct) {
		var museumData = await hypixelService.FetchMuseum(profileId, ct);
		
		if (museumData.IsError) {
			return;
		}
		
		var members = museumData.Value.Members;
		
		var memberIds = await context.ProfileMembers
			.Where(pm => pm.ProfileId == profileId)
			.Select(pm => new { pm.Id, pm.PlayerUuid })
			.ToDictionaryAsync(pm => pm.PlayerUuid, pm => pm.Id, cancellationToken: ct);

		var memberIdList = memberIds.Values.ToList();
		var validIds = new List<Guid>();
		
		foreach (var (playerUuid, memberData) in members) {
			if (!memberIds.TryGetValue(playerUuid, out var memberId)) {
				continue;
			}
			validIds.Add(memberId);
			
			var hash = HashUtility.ComputeSha256Hash(JsonSerializer.Serialize(memberData));
		
			// Remove existing inventory if we have new data, or it hasn't been updated in a while
			var existing = await context.HypixelInventory.FirstOrDefaultAsync(
				h => h.ProfileMemberId == memberId && h.Name == "museum",
				cancellationToken: ct);
			
			if (existing is not null) {
				if (existing.Hash == hash && !existing.HypixelInventoryId.ExtractUnixSeconds().OlderThanDays(2)) return;
				context.HypixelInventory.Remove(existing);
			}

			var inventory = new HypixelInventory() {
				ProfileMemberId = memberId,
				Hash = hash,
				Name = "museum",
			};

			foreach (var (key, item) in memberData.Items) {
				var items = NbtParser.NbtToItems(item.Items.Data);
				foreach (var itemDto in items) {
					if (itemDto is null) continue;
					inventory.Items.Add(GetHypixelItem(item, itemDto, key + ":"));
				}
			}

			for (var index = 0; index < memberData.Special.Count; index++) {
				var item = memberData.Special[index];
				var items = NbtParser.NbtToItems(item.Items.Data);
				foreach (var itemDto in items) {
					if (itemDto is null) continue;
					itemDto.Slot = index.ToString();
					inventory.Items.Add(GetHypixelItem(item, itemDto, "SPECIAL:"));
				}
			}

			await context.HypixelInventory.AddAsync(inventory, ct);
		}
		
		await context.SaveChangesAsync(ct);
		
		await context.ProfileMembers
			.Where(m => memberIdList.Contains(m.Id))
			.ExecuteUpdateAsync(m => m
				.SetProperty(pm => pm.Api.Museum, pm => validIds.Contains(pm.Id)),
				cancellationToken: ct);
		
		var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
		await context.Profiles
			.Where(p => p.ProfileId == profileId)
			.ExecuteUpdateAsync(p => p
				.SetProperty(pr => pr.MuseumLastUpdated, pr => now),
				cancellationToken: ct);
	}
	
	private static HypixelItem GetHypixelItem(MuseumItem item, ItemDto itemDto, string prefix)
	{
		itemDto.Slot = prefix + itemDto.Slot;
		
		itemDto.Attributes ??= new ItemAttributes();
					
		itemDto.Attributes.Extra["museum_donated_time"] = item.DonationTime;
		if (item.FeaturedSlot is not null) {
			itemDto.Attributes.Extra["museum_featured_slot"] = item.FeaturedSlot;
		}
		if (item.Borrowing) {
			itemDto.Attributes.Extra["museum_borrowing"] = true;
		}
		if (item.DonatedAsAChild) {
			itemDto.Attributes.Extra["museum_child"] = true;
		}
		
		return itemDto.ToHypixelItem();
	}
}