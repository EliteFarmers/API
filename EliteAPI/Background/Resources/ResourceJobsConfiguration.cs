using EliteAPI.Configuration.Settings;
using Microsoft.Extensions.Options;
using Quartz;

namespace EliteAPI.Background.Resources;

public class ResourceJobsConfiguration(
	IOptions<ConfigCooldownSettings> cooldowns,
	IOptions<AuctionHouseSettings> auctionSettings)
	: IConfigureOptions<QuartzOptions> {
	private readonly ConfigCooldownSettings _cooldowns = cooldowns.Value;
	private readonly AuctionHouseSettings _auctionSettings = auctionSettings.Value;

	public void Configure(QuartzOptions options) {
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
			.AddTrigger(trigger => {
				trigger.ForJob(itemsJobKey);
				trigger.StartAt(DateTimeOffset.Now.AddSeconds(2));
				trigger.WithSimpleSchedule(schedule => {
					schedule.WithIntervalInSeconds(_cooldowns.ItemsRefreshInterval);
					schedule.RepeatForever();
				});
			});

		var firesalesJobKey = FiresalesUpdateJob.Key;
		options.AddJob<FiresalesUpdateJob>(builder => builder.WithIdentity(firesalesJobKey))
			.AddTrigger(trigger => {
				trigger.ForJob(firesalesJobKey);
				trigger.StartAt(DateTimeOffset.Now.AddSeconds(10));
				trigger.WithSimpleSchedule(schedule => {
					schedule.WithIntervalInSeconds(_cooldowns.ItemsRefreshInterval);
					schedule.RepeatForever();
				});
			});

		var auctionJobKey = AuctionsUpdateJob.Key;
		options.AddJob<AuctionsUpdateJob>(builder => builder.WithIdentity(auctionJobKey))
			.AddTrigger(trigger => {
				trigger.ForJob(auctionJobKey);
				trigger.StartAt(DateTimeOffset.Now.AddSeconds(15));
				trigger.WithSimpleSchedule(schedule => {
					// Additional 20 seconds to ensure cache key check passes
					schedule.WithIntervalInSeconds(_auctionSettings.AuctionsRefreshInterval + 20);
					schedule.RepeatForever();
				});
			});
	}
}