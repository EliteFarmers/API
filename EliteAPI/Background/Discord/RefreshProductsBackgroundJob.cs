using System.Net.Http.Headers;
using EliteAPI.Configuration.Settings;
using EliteAPI.Data;
using EliteAPI.Models.Entities.Monetization;
using EliteAPI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Quartz;
using StackExchange.Redis;

namespace EliteAPI.Background.Discord;

[DisallowConcurrentExecution]
public class RefreshProductsBackgroundJob(
    IConnectionMultiplexer redis,
    ILogger<RefreshProductsBackgroundJob> logger,
    DataContext context,
    IHttpClientFactory httpClientFactory,
    IOptions<ConfigCooldownSettings> coolDowns,
    IMessageService messageService
	) : IJob
{
    public static readonly JobKey Key = new(nameof(RefreshProductsBackgroundJob));
    private const string ClientName = "EliteAPI";
    private readonly string _botToken = Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN") 
                                        ?? throw new Exception("DISCORD_BOT_TOKEN env variable is not set.");
    private readonly string _clientId = Environment.GetEnvironmentVariable("DISCORD_CLIENT_ID") 
                                        ?? throw new Exception("DISCORD_CLIENT_ID env variable is not set.");
    private readonly ConfigCooldownSettings _coolDowns = coolDowns.Value;
    
    private const string DiscordBaseUrl = "https://discord.com/api/v10";
    
	public async Task Execute(IJobExecutionContext executionContext) {
        logger.LogInformation("Fetching products from Discord - {UtcNow}", DateTime.UtcNow);

        if (executionContext.RefireCount > 3) {
            messageService.SendErrorMessage("Failed to fetch Discord products", "Failed to fetch products from Discord");
            return;
        }

        try {
            await RefreshDiscordProducts(executionContext.CancellationToken);
        } catch (Exception e) {
            messageService.SendErrorMessage("Failed to fetch Discord products", e.Message);
            throw new JobExecutionException(msg: "", refireImmediately: true, cause: e);
        }
    }
	
	private async Task RefreshDiscordProducts(CancellationToken ct) {
        var db = redis.GetDatabase();
        if (db.KeyExists("bot:products")) {
            logger.LogInformation("Products are still on cooldown");
            return;
        }
        await db.StringSetAsync("bot:products", "1", TimeSpan.FromSeconds(_coolDowns.DiscordProductsCooldown));
        
        var products = await FetchDiscordProducts(ct);
        
        logger.LogInformation("Fetched {ProductCount} products from Discord", products.Count);

        // Allow retry sooner if no products were found
        if (products.Count == 0) {
            await db.StringGetSetExpiryAsync("bot:products", TimeSpan.FromSeconds(60));
        }
        
        foreach (var discordProduct in products) {
            if (discordProduct.Type == ProductType.SubscriptionGroup) continue;
            if (!ulong.TryParse(discordProduct.Id, out var id)) continue;
            
            var existing = await context.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken: ct);
            
            if (existing is null) {
                context.Products.Add(new Product {
                    Id = id,
                    Type = discordProduct.Type,
                    Category = ProductCategory.None,
                    Name = discordProduct.Name,
                    Slug = discordProduct.Slug,
                    Flags = discordProduct.Flags,
                });
            } else {
                existing.Type = discordProduct.Type;
                existing.Name = discordProduct.Name;
                existing.Slug = discordProduct.Slug;
                existing.Flags = discordProduct.Flags;
            }
        }
        
        await context.SaveChangesAsync(ct);
    }
    
    private async Task<List<DiscordProduct>> FetchDiscordProducts(CancellationToken ct) {
        var url = DiscordBaseUrl + $"/applications/{_clientId}/skus";

        var client = httpClientFactory.CreateClient(ClientName);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bot", _botToken);
        
        var response = await client.GetAsync(url, ct);

        if (!response.IsSuccessStatusCode) {
            logger.LogWarning("Failed to fetch products from Discord: {Reason}", response.ReasonPhrase);
            return [];
        }

        try {
            return await response.Content.ReadFromJsonAsync<List<DiscordProduct>>(cancellationToken: ct) ?? [];
        } catch (Exception e) {
            logger.LogError(e, "Failed to parse bot guilds from Discord");
        }

        return [];
    }
    
    public class DiscordProduct {
        public required string Id { get; set; }
        public ProductType Type { get; set; }
        public required string Name { get; set; }
        public required string Slug { get; set; }
        public ulong Flags { get; set; }
    }
}