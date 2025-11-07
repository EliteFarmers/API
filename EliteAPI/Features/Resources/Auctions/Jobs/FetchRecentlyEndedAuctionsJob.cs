using System.Text;
using EliteAPI.Data;
using EliteAPI.Features.Profiles.Services;
using EliteAPI.Features.Resources.Auctions.Models;
using EliteAPI.Features.Resources.Auctions.Services;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Parsers.Inventories;
using EliteAPI.Utilities;
using EliteFarmers.HypixelAPI;
using Microsoft.EntityFrameworkCore;
using Quartz;

namespace EliteAPI.Features.Resources.Auctions.Jobs;

public class FetchRecentlyEndedAuctionsJob(
	IHypixelApi hypixelApi,
	DataContext context,
	IMemberService memberService,
	VariantKeyGenerator variantKeyGenerator,
	ILogger<FetchRecentlyEndedAuctionsJob> logger) : ISelfConfiguringJob
{
	public static readonly JobKey Key = new(nameof(FetchRecentlyEndedAuctionsJob));

	public static void Configure(IServiceCollectionQuartzConfigurator quartz, ConfigurationManager configuration) {
		quartz.AddJob<FetchRecentlyEndedAuctionsJob>(builder => builder.WithIdentity(Key))
			.AddTrigger(trigger => {
				trigger.ForJob(Key);
				trigger.StartNow();
				trigger.WithSimpleSchedule(schedule => {
					schedule.WithIntervalInSeconds(58);
					schedule.RepeatForever();
				});
			});
	}

	public async Task Execute(IJobExecutionContext executionContext) {
		var fetched = await hypixelApi.FetchAuctionHouseRecentlyEndedAsync(executionContext.CancellationToken);

		if (!fetched.IsSuccessful || !fetched.Content.Success) {
			logger.LogError("Failed to fetch recently ended auctions");
			return;
		}

		var auctions = fetched.Content.Auctions;
		var auctionIds = auctions.Select(a => Guid.Parse(a.Uuid)).ToList();

		var existing = await context.EndedAuctions
			.Where(a => auctionIds.Contains(a.AuctionId))
			.Select(a => a.AuctionId)
			.ToHashSetAsync();

		var newAuctions = 0;
		var profileMemberCache = new Dictionary<(string Player, string Profile), Guid?>();

		foreach (var auction in auctions) {
			if (existing.Contains(Guid.Parse(auction.Uuid))) continue;

			var sellerKey = (auction.Seller, auction.SellerProfileId);
			var buyerKey = (auction.Buyer, auction.BuyerProfileId);

			if (!profileMemberCache.TryGetValue(sellerKey, out var seller)) {
				seller = await memberService.GetProfileMemberId(auction.Seller, auction.SellerProfileId);
				profileMemberCache[sellerKey] = seller;
			}

			if (!profileMemberCache.TryGetValue(buyerKey, out var buyer)) {
				buyer = await memberService.GetProfileMemberId(auction.Buyer, auction.BuyerProfileId);
				profileMemberCache[buyerKey] = buyer;
			}

			var item = NbtParser.NbtToItem(auction.ItemBytes);
			var rarity = DetermineRarity(item);
			var variation = item is not null
				? variantKeyGenerator.Generate(item, rarity)
				: null;
			var variantKey = variation?.ToKey() ?? string.Empty;
			var count = item?.Count > 0 ? (short)item.Count : (short)1;

			var endedAuction = new EndedAuction() {
				AuctionId = Guid.Parse(auction.Uuid),

				SellerUuid = Guid.Parse(auction.Seller),
				SellerProfileUuid = Guid.Parse(auction.SellerProfileId),
				SellerProfileMemberId = seller,

				BuyerUuid = Guid.Parse(auction.Buyer),
				BuyerProfileUuid = Guid.Parse(auction.BuyerProfileId),
				BuyerProfileMemberId = buyer,

				Price = auction.Price,
				Count = count,
				Bin = auction.Bin,
				Timestamp = auction.Timestamp,
				ItemUuid = item?.Uuid is not null ? Guid.Parse(item.Uuid) : null,

				SkyblockId = item?.SkyblockId,
				VariantKey = variantKey,
				Item = Convert.FromBase64String(auction.ItemBytes)
			};

			newAuctions++;
			context.EndedAuctions.Add(endedAuction);
		}
		
		await context.SaveChangesAsync();
		if (newAuctions > 0) {
			logger.LogInformation("Processed {auctionCount} recently ended auctions", newAuctions);
		}
	}

	private static string DetermineRarity(ItemDto? item) {
		if (item?.PetInfo?.Tier is { Length: > 0 } petTier) return petTier.ToUpperInvariant();
		
		var rarityLine = item?.Lore?.Count > 0 ? item!.Lore[^1] : null;
		if (!string.IsNullOrWhiteSpace(rarityLine)) {
			var sanitized = StripColorCodes(rarityLine).Trim();
			if (!string.IsNullOrWhiteSpace(sanitized)) {
				var parts = sanitized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
				if (parts.Length > 0) return parts[0].ToUpperInvariant();
			}
		}
		return "COMMON";
	}

	private static string StripColorCodes(string value) {
		if (string.IsNullOrEmpty(value)) return string.Empty;
		var builder = new StringBuilder(value.Length);
		var skipNext = false;
		foreach (var ch in value) {
			if (skipNext) {
				skipNext = false;
				continue;
			}
			if (ch == '§') {
				skipNext = true;
				continue;
			}
			builder.Append(ch);
		}
		return builder.ToString();
	}
}