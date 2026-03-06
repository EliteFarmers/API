using EliteAPI.Features.Resources.Bazaar;
using Quartz;

namespace EliteAPI.Background.Resources;

public class BazaarSnapshotCleanupJob(BazaarSnapshotCleanupService cleanupService) : IJob
{
	public static readonly JobKey Key = new(nameof(BazaarSnapshotCleanupJob));

	public async Task Execute(IJobExecutionContext context) {
		await cleanupService.DownsampleOldSnapshotsAsync(context.CancellationToken);
	}
}
