using EliteAPI.Features.Resources.Bazaar;
using Quartz;

namespace EliteAPI.Background.Resources;

public class BazaarUpdateJob(BazaarIngestionService bz) : IJob
{
	public static readonly JobKey Key = new(nameof(BazaarUpdateJob));

	public async Task Execute(IJobExecutionContext context) {
		await bz.IngestBazaarDataAsync();
	}
}