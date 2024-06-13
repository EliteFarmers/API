using Microsoft.Extensions.Options;
using Quartz;

namespace EliteAPI.Background.Profiles;

public class HypixelJobsConfiguration : IConfigureOptions<QuartzOptions> {
	public void Configure(QuartzOptions options)
	{
		// Process Contests
		options.AddJob<ProcessContestsBackgroundJob>(builder => {
			builder.WithIdentity(ProcessContestsBackgroundJob.Key);
			builder.StoreDurably();
		});
	}
}