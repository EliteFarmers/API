using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using EliteAPI.Configuration.Settings;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Models.Entities.Discord;
using EliteAPI.Models.Entities.Monetization;
using EliteAPI.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace EliteAPI.Services;

public class MonetizationService(
	DataContext context,
	IHttpClientFactory httpClientFactory,
	IConnectionMultiplexer redis,
	ILogger<DiscordService> logger,
	IBadgeService badgeService,
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
	
	public async Task UpdateProductAsync(ulong productId, UpdateProductDto updateProductDto) {
		var product = await context.Products
			.FirstOrDefaultAsync(x => x.Id == productId);

		if (product is null) return;
		
		product.Category = updateProductDto.Category ?? product.Category;
		product.Icon = updateProductDto.Icon ?? product.Icon;
		product.Description = updateProductDto.Description ?? product.Description;
		
		if (updateProductDto.Features is not null) {
			product.Features.MaxJacobLeaderboards = updateProductDto.Features.MaxJacobLeaderboards ?? product.Features.MaxJacobLeaderboards;
			product.Features.MaxMonthlyEvents = updateProductDto.Features.MaxMonthlyEvents ?? product.Features.MaxMonthlyEvents;
			product.Features.BadgeId = updateProductDto.Features.BadgeId ?? product.Features.BadgeId;
			product.Features.EmbedColors = updateProductDto.Features.EmbedColors ?? product.Features.EmbedColors;
			product.Features.WeightStyles = updateProductDto.Features.WeightStyles ?? product.Features.WeightStyles;
			product.Features.HideShopPromotions = updateProductDto.Features.HideShopPromotions ?? product.Features.HideShopPromotions;
			product.Features.WeightStyleOverride = updateProductDto.Features.WeightStyleOverride ?? product.Features.WeightStyleOverride;
			
			context.Entry(product).Property(p => p.Features).IsModified = true;
		}
		
		await context.SaveChangesAsync();
	}

	public async Task<List<UserEntitlement>> GetUserEntitlementsAsync(ulong userId) {
		return await context.UserEntitlements
			.Where(x => x.AccountId == userId)
			.Include(u => u.Product)
			.ToListAsync();
	}

	public async Task<List<GuildEntitlement>> GetGuildEntitlementsAsync(ulong guildId) {
		return await context.GuildEntitlements
			.Where(x => x.GuildId == guildId)
			.Include(g => g.Product)
			.ToListAsync();
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
		
		var user = await context.Accounts
			.Include(a => a.UserSettings)
			.Include(a => a.Entitlements)
			.ThenInclude(e => e.Product)
			.Include(a => a.MinecraftAccounts)
			.ThenInclude(m => m.Badges)
			.FirstOrDefaultAsync(x => x.Id == userId);
		
		if (user is null) return;
		
		var url = DiscordBaseUrl + $"/applications/{_clientId}/entitlements?user_id={userId}";
		var entitlements = await FetchEntitlementsRecursive(url);

		foreach (var entitlement in entitlements) {
			var existing = user.Entitlements
				.FirstOrDefault(x => x.Id == entitlement.Id);
			
			if (existing is null) {
				var newEntitlement = new UserEntitlement {
					Id = entitlement.Id,
					AccountId = user.Id,
					ProductId = entitlement.ProductId,
					Type = entitlement.Type,
					Deleted = entitlement.Deleted,
					Consumed = entitlement.Consumed,
					StartDate = entitlement.StartsAt,
					EndDate = entitlement.EndsAt,
				};
				user.Entitlements.Add(newEntitlement);
				context.UserEntitlements.Add(newEntitlement);
			} else {
				existing.Type = entitlement.Type;
				existing.Deleted = entitlement.Deleted;
				existing.Consumed = entitlement.Consumed;
				existing.StartDate = entitlement.StartsAt;
				existing.EndDate = entitlement.EndsAt;
			}
		}

		// Remove entitlements that the user no longer has
		foreach (var existing in user.Entitlements) {
			if (entitlements.All(x => x.Id != existing.Id)) {
				existing.Deleted = true;
			}
		}

		if (user.Entitlements.Count > 0) {
			await UpdateUserFeaturesAsync(user);
		}
		
		await context.SaveChangesAsync();
	}

	private async Task UpdateUserFeaturesAsync(EliteAccount account) {
		var hasHideShopPromotions = account.Entitlements
			.Any(x => x is { Active: true, Product.Features.HideShopPromotions: true });
		var hasWeightStyleOverride = account.Entitlements
			.Any(x => x is { Active: true, Product.Features.WeightStyleOverride: true });
		var hasMoreInfoDefault = account.Entitlements
			.Any(x => x is { Active: true, Product.Features.MoreInfoDefault: true });
		
		// Flag the account as having active rewards (or not)
		account.ActiveRewards = hasHideShopPromotions || hasWeightStyleOverride || hasMoreInfoDefault;

		// Disable features if the user doesn't have the entitlement
		account.UserSettings.Features.HideShopPromotions = hasHideShopPromotions && account.UserSettings.Features.HideShopPromotions;
		account.UserSettings.Features.WeightStyleOverride = hasWeightStyleOverride && account.UserSettings.Features.WeightStyleOverride;
		account.UserSettings.Features.MoreInfoDefault = hasMoreInfoDefault && account.UserSettings.Features.MoreInfoDefault;

		if (account.UserSettings.Features.WeightStyle is {} style) {
			// Check if the user has an entitlement for that weight style
			var weightStyle = account.Entitlements
				.FirstOrDefault(x => x.Active && x.Product.Features.WeightStyles?.Contains(style) is true);
			
			// Clear the weight style if the user doesn't have the entitlement
			if (weightStyle is null) {
				account.UserSettings.Features.WeightStyle = null;
			}
		}
		
		if (account.UserSettings.Features.EmbedColor is {} color) {
			// Check if the user has an entitlement for that embed color
			var embedColor = account.Entitlements
				.FirstOrDefault(x => x.Active && x.Product.Features.EmbedColors?.Contains(color) is true);
			
			// Clear the embed color if the user doesn't have the entitlement
			if (embedColor is null) {
				account.UserSettings.Features.EmbedColor = null;
			}
		}
		
		// Check that active badges are still valid
		var primary = account.MinecraftAccounts.FirstOrDefault(x => x.Selected);
		if (primary is not null) {
			var badgeEntitlements = account.Entitlements
				.Where(x => x is { Product.Features.BadgeId : > 0 })
				.GroupBy(x => x.Product.Features.BadgeId)
				.Select(g => new { BadgeId = g.Key!.Value, Active = g.Any(x => x.Active) });
		
			foreach (var badge in badgeEntitlements) {
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
		
		var guild = await context.Guilds
			.Include(x => x.Entitlements)
			.FirstOrDefaultAsync(x => x.Id == guildId);
		
		if (guild is null) return;
		
		var url = DiscordBaseUrl + $"/applications/{_clientId}/entitlements?guild_id={guildId}";
		var entitlements = await FetchEntitlementsRecursive(url);
		
		foreach (var entitlement in entitlements) {
			var existing = guild.Entitlements
				.FirstOrDefault(x => x.Id == entitlement.Id);
			
			if (existing is null) {
				var newEntitlement = new GuildEntitlement {
					Id = entitlement.Id,
					GuildId = guild.Id,
					ProductId = entitlement.ProductId,
					Type = entitlement.Type,
					Deleted = entitlement.Deleted,
					Consumed = entitlement.Consumed,
					StartDate = entitlement.StartsAt,
					EndDate = entitlement.EndsAt,
				};
				
				guild.Entitlements.Add(newEntitlement);
				context.GuildEntitlements.Add(newEntitlement);
			} else {
				existing.Type = entitlement.Type;
				existing.Deleted = entitlement.Deleted;
				existing.Consumed = entitlement.Consumed;
				existing.StartDate = entitlement.StartsAt;
				existing.EndDate = entitlement.EndsAt;
			}
		}
		
		// Remove entitlements that the guild no longer has
		foreach (var existing in guild.Entitlements) {
			if (entitlements.All(x => x.Id != existing.Id)) {
				existing.Deleted = true;
			}
		}

		if (entitlements.Count > 0) {
			await UpdateGuildFeaturesAsync(guild, guild.Entitlements);
		}
		
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