using EliteAPI.Configuration.Settings;
using Microsoft.Extensions.Options;
using Quartz;

namespace EliteAPI.Background.Discord;

public class DiscordJobsConfiguration(IOptions<ConfigCooldownSettings> cooldowns) : IConfigureOptions<QuartzOptions>
{
	private readonly ConfigCooldownSettings _cooldowns = cooldowns.Value;

	public void Configure(QuartzOptions options) {
		// Refresh Bot Guilds
		var guildsKey = RefreshBotGuildsBackgroundJob.Key;
		options.AddJob<RefreshBotGuildsBackgroundJob>(builder => builder.WithIdentity(guildsKey))
			.AddTrigger(trigger => {
				trigger.ForJob(guildsKey);
				trigger.StartAt(DateTimeOffset.Now.AddSeconds(5));
				trigger.WithSimpleSchedule(schedule => {
					schedule.WithIntervalInSeconds(_cooldowns.DiscordGuildsCooldown);
					schedule.RepeatForever();
				});
			});

		var productsKey = RefreshProductsBackgroundJob.Key;
		options.AddJob<RefreshProductsBackgroundJob>(builder => builder.WithIdentity(productsKey))
			.AddTrigger(trigger => {
				trigger.ForJob(productsKey);
				trigger.StartAt(DateTimeOffset.Now.AddSeconds(5));
				trigger.WithSimpleSchedule(schedule => {
					schedule.WithIntervalInSeconds(_cooldowns.DiscordProductsCooldown);
					schedule.RepeatForever();
				});
			});

		var entitlementsKey = RefreshEntitlementsBackgroundJob.Key;
		options.AddJob<RefreshEntitlementsBackgroundJob>(builder => builder.WithIdentity(entitlementsKey))
			.AddTrigger(trigger => {
				trigger.ForJob(entitlementsKey);
				trigger.StartAt(DateTimeOffset.Now.AddSeconds(5));
				trigger.WithSimpleSchedule(schedule => {
					schedule.WithIntervalInSeconds(_cooldowns.EntitlementsRefreshCooldown);
					schedule.RepeatForever();
				});
			});

		var cleanupKey = CleanupRefreshTokens.Key;
		options.AddJob<CleanupRefreshTokens>(builder => builder.WithIdentity(cleanupKey))
			.AddTrigger(trigger => {
				trigger.ForJob(cleanupKey);
				trigger.StartAt(DateTimeOffset.Now.AddSeconds(5));
				trigger.WithSimpleSchedule(schedule => {
					schedule.WithIntervalInHours(24);
					schedule.RepeatForever();
				});
			});

		options.AddJob<RefreshAuthTokenBackgroundTask>(builder => {
			builder.WithIdentity(RefreshAuthTokenBackgroundTask.Key);
			builder.StoreDurably();
		});

		options.AddJob<RefreshUserGuildsBackgroundJob>(builder => {
			builder.WithIdentity(RefreshUserGuildsBackgroundJob.Key);
			builder.StoreDurably();
		});
	}
}