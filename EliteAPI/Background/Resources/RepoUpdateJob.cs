using EliteAPI.Features.Resources.Auctions.Services;
using Quartz;
using SkyblockRepo;

namespace EliteAPI.Background.Resources;

public class RepoUpdateJob(ISkyblockRepoClient repoClient)
	: IJob
{
	public static readonly JobKey Key = new(nameof(RepoUpdateJob));

	public async Task Execute(IJobExecutionContext context) {
		await repoClient.CheckForUpdatesAsync();
	}
}