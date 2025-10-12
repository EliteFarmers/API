using EliteAPI.Background.Resources;
using EliteAPI.Background.Discord;
using EliteAPI.Background.Profiles;
using Quartz;

namespace EliteAPI.Background;

public static class QuartzExtensions {
	public static void AddEliteBackgroundJobs(this IServiceCollection services) {
		services.AddQuartz();

		services.AddQuartzHostedService(options => { options.WaitForJobsToComplete = true; });

		services.ConfigureOptions<DiscordJobsConfiguration>();
		services.ConfigureOptions<HypixelJobsConfiguration>();
		services.ConfigureOptions<ResourceJobsConfiguration>();
	}
}