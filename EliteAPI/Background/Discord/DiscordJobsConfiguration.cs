using EliteAPI.Configuration.Settings;
using Microsoft.Extensions.Options;
using Quartz;

namespace EliteAPI.Background.Discord;

public class DiscordJobsConfiguration(IOptions<ConfigCooldownSettings> cooldowns) : IConfigureOptions<QuartzOptions> 
{
	private readonly ConfigCooldownSettings _cooldowns = cooldowns.Value;
	
	public void Configure(QuartzOptions options)
	{
		// Refresh Bot Guilds
		var key = RefreshBotGuildsBackgroundJob.Key;
		options.AddJob<RefreshBotGuildsBackgroundJob>(builder => builder.WithIdentity(key))
			.AddTrigger(trigger => {
				trigger.ForJob(key);
				trigger.StartNow();
				trigger.WithSimpleSchedule(schedule => {
					schedule.WithIntervalInSeconds(_cooldowns.DiscordGuildsCooldown);
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