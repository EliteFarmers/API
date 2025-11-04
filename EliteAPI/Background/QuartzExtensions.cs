using EliteAPI.Background.Resources;
using EliteAPI.Background.Discord;
using EliteAPI.Background.Profiles;
using EliteAPI.Utilities;
using Quartz;

namespace EliteAPI.Background;

public static class QuartzExtensions
{
	public static void AddEliteBackgroundJobs(this WebApplicationBuilder builder) {
		builder.Services.AddQuartz(options => {
			options.AddSelfConfiguringJobs(builder.Configuration);
		});

		builder.Services.AddQuartzHostedService(options => {
			options.WaitForJobsToComplete = true;
		});

		builder.Services.ConfigureOptions<DiscordJobsConfiguration>();
		builder.Services.ConfigureOptions<HypixelJobsConfiguration>();
		builder.Services.ConfigureOptions<ResourceJobsConfiguration>();
	}
}