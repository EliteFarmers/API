using EliteAPI.Configuration.Settings;
using Microsoft.Extensions.Options;
using Quartz;

namespace EliteAPI.Background.Resources;

public class ResourceJobsConfiguration(IOptions<ConfigCooldownSettings> cooldowns) : IConfigureOptions<QuartzOptions> {
	private readonly ConfigCooldownSettings _cooldowns = cooldowns.Value;
	
	public void Configure(QuartzOptions options)
	{
		// Update Bazaar products
		var bzJobKey = BazaarUpdateJob.Key;
		options.AddJob<BazaarUpdateJob>(builder => builder.WithIdentity(bzJobKey))
			.AddTrigger(trigger => {
				trigger.ForJob(bzJobKey);
				trigger.StartAt(DateTimeOffset.Now.AddSeconds(10));
				trigger.WithSimpleSchedule(schedule => {
					schedule.WithIntervalInSeconds(_cooldowns.BazaarRefreshInterval);
					schedule.RepeatForever();
				});
			});

		var itemsJobKey = ItemsUpdateJob.Key;
		options.AddJob<ItemsUpdateJob>(builder => builder.WithIdentity(itemsJobKey))
			.AddTrigger(trigger =>
			{
				trigger.ForJob(itemsJobKey);
				trigger.StartAt(DateTimeOffset.Now.AddSeconds(2));
				trigger.WithSimpleSchedule(schedule =>
				{
					schedule.WithIntervalInSeconds(_cooldowns.ItemsRefreshInterval);
					schedule.RepeatForever();
				});
			});
	}
}