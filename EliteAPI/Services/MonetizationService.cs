using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using EliteAPI.Configuration.Settings;
using EliteAPI.Data;
using EliteAPI.Models.Entities.Monetization;
using EliteAPI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace EliteAPI.Services;

public class MonetizationService(
	DataContext context,
	IHttpClientFactory httpClientFactory,
	IConnectionMultiplexer redis,
	ILogger<DiscordService> logger,
	IOptions<ConfigCooldownSettings> coolDowns)
	: IMonetizationService 
{
	private const string ClientName = "EliteAPI";
	private const string DiscordBaseUrl = "https://discord.com/api/v10";

	private readonly string _clientId = Environment.GetEnvironmentVariable("DISCORD_CLIENT_ID") 
	                                    ?? throw new Exception("DISCORD_CLIENT_ID env variable is not set.");
	private readonly string _clientSecret = Environment.GetEnvironmentVariable("DISCORD_CLIENT_SECRET") 
	                                        ?? throw new Exception("DISCORD_CLIENT_SECRET env variable is not set.");
	private readonly string _botToken = Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN") 
	                                    ?? throw new Exception("DISCORD_BOT_TOKEN env variable is not set.");
    
	private readonly ConfigCooldownSettings _coolDowns = coolDowns.Value;


	public async Task UpdateProductCategoryAsync(ulong productId, ProductCategory category) {
		var product = await context.Products
			.FirstOrDefaultAsync(x => x.Id == productId);

		if (product is null) return;
		
		product.Category = category;
		
		await context.SaveChangesAsync();
	}

	public async Task<List<UserEntitlement>> GetUserEntitlementsAsync(ulong userId) {
		return await context.UserEntitlements
			.Where(x => x.AccountId == userId)
			.ToListAsync();
	}

	public async Task<List<GuildEntitlement>> GetGuildEntitlementsAsync(ulong guildId) {
		return await context.GuildEntitlements
			.Where(x => x.GuildId == guildId)
			.ToListAsync();
	}
	
	public async Task FetchUserEntitlementsAsync(ulong userId) {
		var key = $"user:entitlements:{userId}";
		var db = redis.GetDatabase();
		
		if (await db.KeyExistsAsync(key)) return;
		await db.StringSetAsync(key, "1", TimeSpan.FromSeconds(_coolDowns.EntitlementsRefreshCooldown));
		
		var user = await context.Accounts
			.Include(x => x.Entitlements)
			.FirstOrDefaultAsync(x => x.Id == userId);
		
		if (user is null) return;
		
		var url = DiscordBaseUrl + $"/applications/{_clientId}/entitlements?user_id={userId}";
		var entitlements = await FetchEntitlementsRecursive(url);

		foreach (var entitlement in entitlements) {
			var existing = user.Entitlements
				.FirstOrDefault(x => x.ProductId == entitlement.ProductId);
			
			if (existing is null) {
				user.Entitlements.Add(new UserEntitlement {
					ProductId = entitlement.ProductId,
					Type = entitlement.Type,
					Deleted = entitlement.Deleted,
					Consumed = entitlement.Consumed,
					StartDate = entitlement.StartsAt,
					EndDate = entitlement.EndsAt,
				});
			} else {
				existing.Type = entitlement.Type;
				existing.Deleted = entitlement.Deleted;
				existing.Consumed = entitlement.Consumed;
				existing.StartDate = entitlement.StartsAt;
				existing.EndDate = entitlement.EndsAt;
			}
		}
		
		await context.SaveChangesAsync();
	}
	
	public async Task FetchGuildEntitlementsAsync(ulong guildId) {
		var key = $"guild:entitlements:{guildId}";
		var db = redis.GetDatabase();
		
		if (await db.KeyExistsAsync(key)) return;
		await db.StringSetAsync(key, "1", TimeSpan.FromSeconds(_coolDowns.EntitlementsRefreshCooldown));
		
		var guild = await context.Guilds
			.Include(x => x.Entitlements)
			.FirstOrDefaultAsync(x => x.Id == guildId);
		
		if (guild is null) return;
		
		var url = DiscordBaseUrl + $"/applications/{_clientId}/entitlements?guild_id={guildId}";
		var entitlements = await FetchEntitlementsRecursive(url);
		
		foreach (var entitlement in entitlements) {
			var existing = guild.Entitlements
				.FirstOrDefault(x => x.ProductId == entitlement.ProductId);
			
			if (existing is null) {
				guild.Entitlements.Add(new GuildEntitlement {
					ProductId = entitlement.ProductId,
					Type = entitlement.Type,
					Deleted = entitlement.Deleted,
					Consumed = entitlement.Consumed,
					StartDate = entitlement.StartsAt,
					EndDate = entitlement.EndsAt,
				});
			} else {
				existing.Type = entitlement.Type;
				existing.Deleted = entitlement.Deleted;
				existing.Consumed = entitlement.Consumed;
				existing.StartDate = entitlement.StartsAt;
				existing.EndDate = entitlement.EndsAt;
			}
		}
		
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