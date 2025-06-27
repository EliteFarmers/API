using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using EliteAPI.Configuration.Settings;
using EliteAPI.Data;
using EliteAPI.Features.Monetization.Models;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Models.Entities.Discord;
using EliteAPI.Models.Entities.Monetization;
using EliteAPI.Services.Interfaces;
using FastEndpoints;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace EliteAPI.Features.Monetization.Services;

[RegisterService<IMonetizationService>(LifeTime.Scoped)]
public class MonetizationService(
	DataContext context,
	IHttpClientFactory httpClientFactory,
	IConnectionMultiplexer redis,
	ILogger<MonetizationService> logger,
	IBadgeService badgeService,
    IMessageService messageService,
	IOptions<ConfigCooldownSettings> coolDowns)
	: IMonetizationService 
{
	private const string ClientName = "EliteAPI";
	private const string DiscordBaseUrl = "https://discord.com/api/v10";

	private readonly string _clientId = Environment.GetEnvironmentVariable("DISCORD_CLIENT_ID") 
	                                    ?? throw new Exception("DISCORD_CLIENT_ID env variable is not set.");
	private readonly string _botToken = Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN") 
	                                    ?? throw new Exception("DISCORD_BOT_TOKEN env variable is not set.");
    
	private readonly ConfigCooldownSettings _coolDowns = coolDowns.Value;
	
	public async Task UpdateProductAsync(ulong productId, EditProductDto editProductDto) {
		var product = await context.Products
			.FirstOrDefaultAsync(x => x.Id == productId);

		if (product is null) return;
		
		product.Description = editProductDto.Description ?? product.Description;
		product.Price = editProductDto.Price ?? product.Price;
		
		if (!product.Available && editProductDto.Available is true) {
			product.ReleasedAt = DateTimeOffset.UtcNow;
		}
		
		product.Available = editProductDto.Available ?? product.Available;

		if (!string.IsNullOrWhiteSpace(editProductDto.ReleasedAt) && long.TryParse(editProductDto.ReleasedAt, out var unixTime)) {
			try {
				var releasedAt = DateTimeOffset.FromUnixTimeSeconds(unixTime);
				product.ReleasedAt = releasedAt;
			} catch (Exception e) {
				logger.LogError(e, "Failed to parse releasedAt for product {ProductId}", productId);
			}
		} 
		
		if (editProductDto.Features is not null) {
			product.Features.MaxJacobLeaderboards = editProductDto.Features.MaxJacobLeaderboards ?? product.Features.MaxJacobLeaderboards;
			product.Features.MaxMonthlyEvents = editProductDto.Features.MaxMonthlyEvents ?? product.Features.MaxMonthlyEvents;
			product.Features.BadgeId = editProductDto.Features.BadgeId ?? product.Features.BadgeId;
			product.Features.EmbedColors = editProductDto.Features.EmbedColors ?? product.Features.EmbedColors;
			product.Features.WeightStyles = editProductDto.Features.WeightStyles ?? product.Features.WeightStyles;
			product.Features.HideShopPromotions = editProductDto.Features.HideShopPromotions ?? product.Features.HideShopPromotions;
			product.Features.WeightStyleOverride = editProductDto.Features.WeightStyleOverride ?? product.Features.WeightStyleOverride;
			product.Features.MoreInfoDefault = editProductDto.Features.MoreInfoDefault ?? product.Features.MoreInfoDefault;
			product.Features.CustomEmoji = editProductDto.Features.CustomEmoji ?? product.Features.CustomEmoji;
			
			context.Entry(product).Property(p => p.Features).IsModified = true;
		}
		
		await context.SaveChangesAsync();
	}

	public async Task<List<ProductAccess>> GetEntitlementsAsync(ulong entityId)
	{
		return await context.ProductAccesses
			.Where(x => x.GuildId == entityId || x.UserId == entityId)
			.Include(u => u.Product)
			.ToListAsync();
	}

	public async Task GrantProductAccessAsync(ulong userId, ulong productId)
	{
		// Get free, available product with this ID
		// Only durable products are eligible for free access
		var product = await context.Products
			.Where(x => x.Available && x.Price == 0 && x.Type == ProductType.Durable)
			.FirstOrDefaultAsync(p => p.Id == productId);

		if (product is null) {
			return;
		}
		
		// Check if the user already has access to this product
		var existingAccess = await context.ProductAccesses
			.FirstOrDefaultAsync(pa => pa.UserId == userId && 
			                            pa.ProductId == productId && 
			                            !pa.Revoked);
		
		if (existingAccess is not null) {
			// User already has access, no need to grant again
			return;
		}

		var order = new ShopOrder()
		{
			BuyerId = userId,
			Provider = PaymentProvider.Free,
			ProviderTransactionId = Guid.NewGuid().ToString(), // Use a unique ID for free orders
			Status = OrderStatus.Completed,
			RecipientId = userId,
		};
		context.ShopOrders.Add(order);
		
		// Create a new ProductAccess entry
		var access = new ProductAccess {
			ProductId = productId,
			StartDate = DateTimeOffset.UtcNow,
			EndDate = null, // Lifetime access
			Consumed = false,
			Revoked = false,
			UserId = userId,
			SourceOrderId = order.Id,
		};
		context.ProductAccesses.Add(access);
		
		messageService.SendClaimMessage(
			userId.ToString(),
			productId.ToString()
		);
		
		await context.SaveChangesAsync();
	}

	public async Task<ActionResult> GrantTestEntitlementAsync(ulong targetId, ulong productId, EntitlementTarget target = EntitlementTarget.User) {
		var client = httpClientFactory.CreateClient(ClientName);
		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bot", _botToken);
		
		var body = new {
			sku_id = productId,
			owner_id = targetId,
			owner_type = target,
		};
		
		var jsonContent = JsonSerializer.Serialize(body);
		
		var url = DiscordBaseUrl + $"/applications/{_clientId}/entitlements";
		var response = await client.PostAsync(url, new StringContent(jsonContent, Encoding.UTF8, "application/json"));

		if (!response.IsSuccessStatusCode) {
			return new StatusCodeResult((int) response.StatusCode);
		}

		if (target == EntitlementTarget.Guild) {
			await FetchGuildEntitlementsAsync(targetId);
		} else {
			await FetchUserEntitlementsAsync(targetId);
		}

		return new OkResult();
	}

	public async Task<ActionResult> RemoveTestEntitlementAsync(ulong targetId, ulong entitlementId, EntitlementTarget target = EntitlementTarget.User) {
		var client = httpClientFactory.CreateClient(ClientName);
		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bot", _botToken);
		
		var url = DiscordBaseUrl + $"/applications/{_clientId}/entitlements/{entitlementId}";
		var response = await client.DeleteAsync(url);
		
		if (!response.IsSuccessStatusCode) {
			return new StatusCodeResult((int) response.StatusCode);
		}
		
		if (target == EntitlementTarget.Guild) {
			await FetchGuildEntitlementsAsync(targetId);
		} else {
			await FetchUserEntitlementsAsync(targetId);
		}

		return new OkResult();
	}

	public async Task FetchUserEntitlementsAsync(ulong userId) {
		var key = $"user:entitlements:{userId}";
		var db = redis.GetDatabase();
		
		if (await db.KeyExistsAsync(key)) return;
		await db.StringSetAsync(key, "1", TimeSpan.FromSeconds(_coolDowns.ManualEntitlementsRefreshCooldown));
		
		await SyncDiscordEntitlementsAsync(userId, false);
		
		var user = await context.Accounts
			.Include(a => a.UserSettings)
			.Include(a => a.ProductAccesses)
			.ThenInclude(e => e.Product)
			.ThenInclude(p => p.WeightStyles)
			.Include(a => a.MinecraftAccounts)
			.ThenInclude(m => m.Badges)
			.FirstOrDefaultAsync(x => x.Id == userId);
		
		if (user is null) return;
		
		if (user.ProductAccesses.Count > 0) {
			await UpdateUserFeaturesAsync(user);
		}
		
		await context.SaveChangesAsync();
	}

	private async Task UpdateUserFeaturesAsync(EliteAccount account) {
		var hasHideShopPromotions = account.ProductAccesses
			.Any(x => x is { IsActive: true, Product.Features.HideShopPromotions: true });
		var hasWeightStyleOverride = account.ProductAccesses
			.Any(x => x is { IsActive: true, Product.Features.WeightStyleOverride: true });
		var hasMoreInfoDefault = account.ProductAccesses
			.Any(x => x is { IsActive: true, Product.Features.MoreInfoDefault: true });
		var hasCustomEmoji = account.ProductAccesses
			.Any(x => x is { IsActive: true, Product.Features.CustomEmoji: true });
		
		// Flag the account as having active rewards (or not)
		account.ActiveRewards = hasHideShopPromotions || hasWeightStyleOverride || hasMoreInfoDefault;

		// Disable features if the user doesn't have the entitlement
		account.UserSettings.Features.HideShopPromotions = hasHideShopPromotions && account.UserSettings.Features.HideShopPromotions;
		account.UserSettings.Features.WeightStyleOverride = hasWeightStyleOverride && account.UserSettings.Features.WeightStyleOverride;
		account.UserSettings.Features.MoreInfoDefault = hasMoreInfoDefault && account.UserSettings.Features.MoreInfoDefault;

		if (account.UserSettings.WeightStyleId is {} style) {
			// Check if the user has an entitlement for that weight style
			var validStyle = account.ProductAccesses.Any(ue => ue.IsActive && ue.HasWeightStyle(style));

			if (!validStyle) {
				account.UserSettings.WeightStyleId = null;
				account.UserSettings.WeightStyle = null;
			} else {
				account.ActiveRewards = true;
			}
		}
		
		if (account.UserSettings.LeaderboardStyleId is {} lbStyle) {
			// Check if the user has an entitlement for that leaderboard style
			var validStyle = account.ProductAccesses.Any(ue => ue.IsActive && ue.HasWeightStyle(lbStyle));

			if (!validStyle) {
				account.UserSettings.LeaderboardStyleId = null;
				account.UserSettings.LeaderboardStyle = null;
			} else {
				account.ActiveRewards = true;
			}
		}
		
		if (account.UserSettings.Features.EmbedColor is {} color) {
			// Check if the user has an entitlement for that embed color
			var validEmbedColor = account.ProductAccesses
				.Any(x => x.IsActive && x.Product.Features.EmbedColors?.Contains(color) is true);
			
			// Clear the embed color if the user doesn't have the entitlement
			if (!validEmbedColor) {
				account.UserSettings.Features.EmbedColor = null;
			} else {
				account.ActiveRewards = true;
			}
		}

		if (account.UserSettings.Suffix is not null)
		{
			// Clear the suffix if the user doesn't have the entitlement
			if (!hasCustomEmoji) {
				account.UserSettings.Suffix = null;
			} else {
				account.ActiveRewards = true;
			}
		}
		
		if (account.UserSettings.EmojiUrl is {} emojiUrl) {
			// Check if the user has an entitlement for that embed color
			var validEmojiUrl = account.ProductAccesses
				.Any(x => 
					x.IsActive 
					&& x.Product.WeightStyles.Exists(
						s => s.NameStyle?.Emojis.Exists(e => e.Url == emojiUrl) is true)
					);
			
			// Clear the embed color if the user doesn't have the entitlement
			if (!validEmojiUrl) {
				account.UserSettings.EmojiUrl = null;
			} else {
				account.ActiveRewards = true;
			}
		}
		
		// Check that active badges are still valid
		var primary = account.MinecraftAccounts.FirstOrDefault(x => x.Selected);
		if (primary is not null) {
			var badgeProductAccesses = account.ProductAccesses
				.Where(x => x is { Product.Features.BadgeId : > 0 })
				.GroupBy(x => x.Product.Features.BadgeId)
				.Select(g => new { BadgeId = g.Key!.Value, Active = g.Any(x => x.IsActive) });
		
			foreach (var badge in badgeProductAccesses) {
				switch (badge.Active) {
					case true when primary.Badges.All(x => x.BadgeId != badge.BadgeId):
						account.ActiveRewards = true;
						await badgeService.AddBadgeToUser(primary.Id, badge.BadgeId);
						break;
					case false when primary.Badges.Any(x => x.BadgeId == badge.BadgeId):
						await badgeService.RemoveBadgeFromUser(primary.Id, badge.BadgeId);
						break;
				}
			}
		}
		
		context.UserSettings.Update(account.UserSettings);
		await context.SaveChangesAsync();
	}
	
	public async Task FetchGuildEntitlementsAsync(ulong guildId) {
		var key = $"guild:entitlements:{guildId}";
		var db = redis.GetDatabase();
		
		if (await db.KeyExistsAsync(key)) return;
		await db.StringSetAsync(key, "1", TimeSpan.FromSeconds(_coolDowns.ManualEntitlementsRefreshCooldown));

		await SyncDiscordEntitlementsAsync(guildId, true);

		// if (entitlements.Count > 0) {
		// 	await UpdateGuildFeaturesAsync(guild, guild.Entitlements);
		// }
		
		await context.SaveChangesAsync();
	}
	
	private async Task UpdateGuildFeaturesAsync(Guild guild, List<GuildEntitlement> entitlements) {
		if (guild.Features.Locked || entitlements.Count == 0) return;
		
		var maxLeaderboards = guild.Features.JacobLeaderboard?.MaxLeaderboards ?? 0;
		var maxEvents = guild.Features.EventSettings?.MaxMonthlyEvents ?? 0;

		var currentLeaderboards = 0;
		var currentEvents = 0;

		foreach (var entitlement in entitlements) {
			if (!entitlement.Active) continue;

			var features = entitlement.Product.Features;
			if (features is { MaxMonthlyEvents: > 0 }) {
				currentEvents = Math.Max(features.MaxMonthlyEvents.Value, currentEvents);
			}
            
			if (features is { MaxJacobLeaderboards: > 0 }) {
				currentLeaderboards = Math.Max(features.MaxJacobLeaderboards.Value, currentLeaderboards);
			}
		}

		if (currentLeaderboards == maxLeaderboards && currentEvents == maxEvents) {
			return;
		}
        
		guild.Features.JacobLeaderboard ??= new GuildJacobLeaderboardFeature();
		guild.Features.JacobLeaderboard.MaxLeaderboards = currentLeaderboards;
		guild.Features.EventSettings ??= new GuildEventSettings();
		guild.Features.EventSettings.MaxMonthlyEvents = currentEvents;
            
		context.Entry(guild).Property(p => p.Features).IsModified = true;
		context.Guilds.Update(guild);
        
		await context.SaveChangesAsync();
	}
	
	public async Task SyncDiscordEntitlementsAsync(ulong entityId, bool isGuild)
	{
	    // Fetch Discord entitlements for the user or guild
	    var discordEntitlements = await FetchDiscordEntitlements(entityId, isGuild);

	    foreach (var discordEntitlement in discordEntitlements)
	    {
	        var providerId = discordEntitlement.Id.ToString();

	        // Create or update the ShopOrder based on the Discord entitlement
	        var order = await context.ShopOrders.FirstOrDefaultAsync(o => o.ProviderTransactionId == providerId);
	        if (order is null)
	        {
	            order = new ShopOrder
	            {
	                Provider = PaymentProvider.Discord,
	                ProviderTransactionId = providerId,
	                BuyerId = discordEntitlement.UserId ?? entityId, 
	                Status = OrderStatus.Completed,
	            };
	            
	            // Set recipient based on whether it's a guild or user entitlement
	            if (discordEntitlement.GuildId is not null) {
	                order.RecipientGuildId = discordEntitlement.GuildId;
	            } else {
	                order.RecipientId = discordEntitlement.UserId;
	            }

	            context.ShopOrders.Add(order);
	        }
	        
	        // Create/update ProductAccess entitlement
	        var access = await context.ProductAccesses.FirstOrDefaultAsync(pa => pa.SourceOrderId == order.Id);
	        if (access is null)
	        {
	            access = new ProductAccess
	            {
	                ProductId = discordEntitlement.ProductId,
	                SourceOrderId = order.Id,
	            };

	            // Set the UserId or GuildId based on the entitlement type
	            if (discordEntitlement.GuildId is not null) {
	                access.GuildId = discordEntitlement.GuildId;
	            } else {
	                access.UserId = discordEntitlement.UserId;
	                
	                messageService.SendPurchaseMessage(
		                discordEntitlement.UserId.ToString() ?? string.Empty,
		                discordEntitlement.ProductId.ToString()
		            );
	            }

	            context.ProductAccesses.Add(access);
	        }

	        // Update status and dates
	        access.StartDate = discordEntitlement.StartsAt ?? DateTimeOffset.MinValue;
	        access.EndDate = discordEntitlement.EndsAt;
	        access.Consumed = discordEntitlement.Consumed;
	        if (discordEntitlement.Deleted)
	        {
		        access.Revoked = discordEntitlement.Deleted;
	        }
	        
	        if (access.Revoked) {
	            order.Status = OrderStatus.Refunded;
	        }
	    }
	    await context.SaveChangesAsync();
	}

	private async Task<List<DiscordEntitlement>> FetchDiscordEntitlements(ulong entityId, bool isGuild)
	{
		var url = DiscordBaseUrl + $"/applications/{_clientId}/entitlements";
		if (isGuild) {
			url += $"?guild_id={entityId}";
		} else {
			url += $"?user_id={entityId}";
		}
		
		return await FetchEntitlementsRecursive(url);
	}

	private async Task<List<DiscordEntitlement>> FetchEntitlementsRecursive(string url, List<DiscordEntitlement>? entitlements = null, ulong? after = null) {
		var client = httpClientFactory.CreateClient(ClientName);
		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bot", _botToken);

		var response = (after is not null) 
			? await client.GetAsync(url + $"&after={after}")
			: await client.GetAsync(url);

		if (!response.IsSuccessStatusCode) {
			logger.LogWarning("Failed to fetch entitlements from Discord: {Reason}", response.ReasonPhrase);
			return entitlements ?? [];
		}

		try {
			var newEntitlements = await response.Content.ReadFromJsonAsync<List<DiscordEntitlement>>() ?? [];
			entitlements ??= [];
			entitlements.AddRange(newEntitlements);

			var next = entitlements.LastOrDefault()?.ProductId;
			
			if (next is not null && newEntitlements.Count > 80) {
				return await FetchEntitlementsRecursive(url, entitlements, next);
			}

			return entitlements;
		} catch (Exception e) {
			logger.LogError(e, "Failed to parse entitlements from Discord");
		}

		return [];
	}
}

// https://discord.com/developers/docs/monetization/entitlements#entitlement-object-entitlement-types
public class DiscordEntitlement {
	public ulong Id { get; set; }
	public EntitlementType Type { get; set; }

	[JsonPropertyName("sku_id")]
	public ulong ProductId { get; set; }
	
	[JsonPropertyName("user_id")]
	public ulong? UserId { get; set; }
	[JsonPropertyName("guild_id")]
	public ulong? GuildId { get; set; }

	public bool Deleted { get; set; }
	public bool Consumed { get; set; }
	
	[JsonPropertyName("starts_at")]
	public DateTimeOffset? StartsAt { get; set; }
	[JsonPropertyName("ends_at")]
	public DateTimeOffset? EndsAt { get; set; }
}