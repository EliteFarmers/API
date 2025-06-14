using EliteAPI.Features.Resources.Auctions.Services;
using Quartz;

namespace EliteAPI.Background.Resources;

public class AuctionsUpdateJob(AuctionsIngestionService auctionsIngestionService) : IJob
{
    public static readonly JobKey Key = new(nameof(AuctionsUpdateJob));

    public async Task Execute(IJobExecutionContext context)
    {
	    await auctionsIngestionService.TriggerUpdate();
    }
}