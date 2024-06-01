using EliteAPI.Background.Discord;
using EliteAPI.Config.Settings;
using Microsoft.Extensions.Options;
using Quartz;

namespace EliteAPI.Background.Profiles;

public class HypixelJobsConfiguration(IOptions<ConfigCooldownSettings> cooldowns) : IConfigureOptions<QuartzOptions> {
	private readonly ConfigCooldownSettings _cooldowns = cooldowns.Value;

	public void Configure(QuartzOptions options)
	{
		// Process Contests
		options.AddJob<ProcessContestsBackgroundJob>(builder => {
			builder.WithIdentity(ProcessContestsBackgroundJob.Key);
			builder.StoreDurably();
		});
	}
}