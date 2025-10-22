using System.Net.Http.Headers;
using EliteAPI.Configuration.Settings;
using EliteAPI.Data;
using EliteAPI.Features.Guilds.Services;
using EliteAPI.Features.Images.Services;
using EliteAPI.Models.DTOs.Incoming;
using EliteAPI.Models.Entities.Discord;
using EliteAPI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Quartz;
using StackExchange.Redis;

namespace EliteAPI.Background.Discord;

[DisallowConcurrentExecution]
public class RefreshBotGuildsBackgroundJob(
	IConnectionMultiplexer redis,
	ILogger<RefreshBotGuildsBackgroundJob> logger,
	DataContext context,
	IHttpClientFactory httpClientFactory,
	IOptions<ConfigCooldownSettings> coolDowns,
	IMessageService messageService,
	IGuildImageService guildImageService,
	IImageService imageService
) : IJob
{
	public static readonly JobKey Key = new(nameof(RefreshBotGuildsBackgroundJob));
	private const string ClientName = "EliteAPI";

	private readonly string _botToken = Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN")
	                                    ?? throw new Exception("DISCORD_BOT_TOKEN env variable is not set.");

	private readonly ConfigCooldownSettings _coolDowns = coolDowns.Value;

	private const string DiscordBaseUrl = "https://discord.com/api/v10";


	public async Task Execute(IJobExecutionContext executionContext) {
		logger.LogInformation("Fetching bot guilds from Discord - {UtcNow}", DateTime.UtcNow);

		if (executionContext.RefireCount > 3) {
			messageService.SendErrorMessage("Failed to fetch Discord Guilds",
				"Failed to fetch bot guilds from Discord");
			return;
		}

		try {
			await RefreshBotGuilds(executionContext.CancellationToken);
		}
		catch (Exception e) {
			messageService.SendErrorMessage("Failed to fetch Discord Guilds", e.Message);
			throw new JobExecutionException("", refireImmediately: true, cause: e);
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
		if (guilds.Count == 0) await db.StringGetSetExpiryAsync("bot:guilds", TimeSpan.FromSeconds(60));

		foreach (var guild in guilds) {
			var existingGuild = await context.Guilds
				.Include(g => g.Icon)
				.FirstOrDefaultAsync(g => g.Id == guild.Id, ct);

			if (existingGuild is null) {
				var icon = guild.Icon is null
					? null
					: await guildImageService.UpdateGuildIconAsync(guild.Id, guild.Icon);

				context.Guilds.Add(new Guild {
					Id = guild.Id,
					Name = guild.Name,
					Icon = icon,
					BotPermissions = guild.Permissions,
					HasBot = true,
					DiscordFeatures = guild.Features,
					MemberCount = guild.MemberCount
				});
			}
			else {
				existingGuild.Name = guild.Name;
				existingGuild.BotPermissions = guild.Permissions;
				existingGuild.DiscordFeatures = guild.Features;
				existingGuild.MemberCount = guild.MemberCount;
				existingGuild.HasBot = true;

				if (guild.Icon is not null && guild.Icon != existingGuild.Icon?.Hash) {
					existingGuild.Icon =
						await guildImageService.UpdateGuildIconAsync(guild.Id, guild.Icon, existingGuild.Icon);
					await Task.Delay(1500, ct);
				}
				else if (guild.Icon is null && existingGuild.Icon is not null) {
					await imageService.DeleteImageVariantsAsync(existingGuild.Icon);
					context.Images.Remove(existingGuild.Icon);
					existingGuild.Icon = null;
				}

				existingGuild.LastUpdated = DateTimeOffset.UtcNow;
			}
		}

		await context.SaveChangesAsync(ct);

		// Flag guilds that the bot is no longer in
		await context.Guilds.Where(g => g.HasBot && g.LastUpdated < DateTimeOffset.UtcNow.AddHours(-1))
			.ForEachAsync(g => g.HasBot = false, ct);
	}

	private async Task<List<DiscordGuild>> FetchBotGuildsRecursive(string? guildId, CancellationToken ct,
		List<DiscordGuild>? guildList = null) {
		var url = DiscordBaseUrl + "/users/@me/guilds?with_counts=true";

		if (guildId is not null) url += "&after=" + guildId;
		guildList ??= [];

		var client = httpClientFactory.CreateClient(ClientName);
		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bot", _botToken);

		var response = await client.GetAsync(url, ct);

		if (!response.IsSuccessStatusCode) {
			logger.LogWarning("Failed to fetch bot guilds from Discord: {Reason}", response.ReasonPhrase);
			return guildList;
		}

		try {
			var list = await response.Content.ReadFromJsonAsync<List<DiscordGuild>>(ct);

			if (list is null || list.Count == 0) {
				if (guildList.Count == 0) logger.LogWarning("Bot is not in any guilds");
				return guildList;
			}

			guildList.AddRange(list);

			// Wait for 10 seconds to avoid rate limiting
			await Task.Delay(10000, ct);

			return await FetchBotGuildsRecursive(list.Last().Id.ToString(), ct, guildList);
		}
		catch (Exception e) {
			logger.LogError(e, "Failed to parse bot guilds from Discord");
		}

		return guildList;
	}
}