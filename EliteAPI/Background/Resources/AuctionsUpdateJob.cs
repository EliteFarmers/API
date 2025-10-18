using EliteAPI.Features.Resources.Auctions.Services;
using Quartz;
using SkyblockRepo;

namespace EliteAPI.Background.Resources;

public class AuctionsUpdateJob(AuctionsIngestionService auctionsIngestionService, ISkyblockRepoClient repoClient)
	: IJob
{
	public static readonly JobKey Key = new(nameof(AuctionsUpdateJob));

	public async Task Execute(IJobExecutionContext context) {
		await auctionsIngestionService.TriggerUpdate();
		await repoClient.CheckForUpdatesAsync();
	}
}