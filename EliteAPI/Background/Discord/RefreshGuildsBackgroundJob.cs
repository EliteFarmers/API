using System.Net.Http.Headers;
using EliteAPI.Config.Settings;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Incoming;
using EliteAPI.Models.Entities.Events;
using EliteAPI.Services.DiscordService;
using EliteAPI.Services.MessageService;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Quartz;
using StackExchange.Redis;

namespace EliteAPI.Background.Discord;

[DisallowConcurrentExecution]
public class RefreshGuildsBackgroundJob(
	IDiscordService discordService,
    IConnectionMultiplexer redis,
    ILogger<RefreshGuildsBackgroundJob> logger,
    DataContext context,
    IHttpClientFactory httpClientFactory,
    IOptions<ConfigCooldownSettings> coolDowns,
    IMessageService messageService
	) : IJob
{
    public readonly JobKey Key = new(nameof(RefreshGuildsBackgroundJob));
    private const string ClientName = "EliteAPI";
    private readonly string _botToken = Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN") 
                                        ?? throw new Exception("DISCORD_BOT_TOKEN env variable is not set.");

    private readonly ConfigCooldownSettings _coolDowns = coolDowns.Value;
    
    private const string DiscordBaseUrl = "https://discord.com/api/v10";

    
	public async Task Execute(IJobExecutionContext executionContext) {
        logger.LogInformation("Fetching bot guilds from Discord - {UtcNow}", DateTime.UtcNow);

        if (executionContext.RefireCount > 3) {
            messageService.SendErrorMessage("Failed to fetch Discord Guilds", "Failed to fetch bot guilds from Discord");
            return;
        }

        try {
            await RefreshBotGuilds(executionContext.CancellationToken);
        } catch (Exception e) {
            messageService.SendErrorMessage("Failed to fetch Discord Guilds", e.Message);
            throw new JobExecutionException(msg: "", refireImmediately: true, cause: e);
        }
    }
	
	private async Task RefreshBotGuilds(CancellationToken ct) {
        var db = redis.GetDatabase();
        if (db.KeyExists("bot:guilds")) {
            logger.LogInformation("Guilds are still on cooldown");
            return;
        }
        await db.StringSetAsync("bot:guilds", "1", TimeSpan.FromSeconds(_coolDowns.DiscordGuildsCooldown));
        
        var guilds = await FetchBotGuildsRecursive(null, ct);
        
        logger.LogInformation("Fetched {GuildCount} guilds from Discord", guilds.Count);

        // Allow retry sooner if no guilds were found
        if (guilds.Count == 0) {
            await db.StringGetSetExpiryAsync("bot:guilds", TimeSpan.FromSeconds(60));
        }
        
        var existing = await context.Guilds.ToListAsync(cancellationToken: ct);
        
        foreach (var guild in guilds) {
            var existingGuild = existing.FirstOrDefault(g => g.Id == guild.Id);
            if (existingGuild is null) {
                context.Guilds.Add(new Guild {
                    Id = guild.Id,
                    Name = guild.Name,
                    Icon = guild.Icon,
                    BotPermissions = guild.Permissions,
                    BotPermissionsNew = guild.PermissionsNew,
                    DiscordFeatures = guild.Features,
                    MemberCount = guild.MemberCount
                });
            } else {
                existingGuild.Name = guild.Name;
                existingGuild.Icon = guild.Icon;
                existingGuild.BotPermissions = guild.Permissions;
                existingGuild.BotPermissionsNew = guild.PermissionsNew;
                existingGuild.DiscordFeatures = guild.Features;
                existingGuild.MemberCount = guild.MemberCount;
            }
            
            await db.StringSetAsync($"bot:guild:{guild.Id}", guild.Permissions, TimeSpan.FromSeconds(_coolDowns.DiscordGuildsCooldown), When.Always);
        }
        
        await context.SaveChangesAsync(ct);
    }
    
    private async Task<List<DiscordGuild>> FetchBotGuildsRecursive(string? guildId, CancellationToken ct, List<DiscordGuild>? guildList = null) {
        var url = DiscordBaseUrl + "/users/@me/guilds?with_counts=true";
        
        if (guildId is not null) {
            url += "&after=" + guildId;
        }
        guildList ??= [];
        
        var client = httpClientFactory.CreateClient(ClientName);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bot", _botToken);
        
        var response = await client.GetAsync(url, ct);

        if (!response.IsSuccessStatusCode) {
            logger.LogWarning("Failed to fetch bot guilds from Discord");
            return guildList;
        }

        try {
            var list = await response.Content.ReadFromJsonAsync<List<DiscordGuild>>(cancellationToken: ct);

            if (list is null || list.Count == 0) {
                if (guildList.Count == 0) {
                    logger.LogWarning("Bot is not in any guilds");
                }
                return guildList;
            }
            
            guildList.AddRange(list);
            
            return await FetchBotGuildsRecursive(list.Last().Id.ToString(), ct, guildList);
        } catch (Exception e) {
            logger.LogError(e, "Failed to parse bot guilds from Discord");
        }

        return guildList;
    }
}