using EliteAPI.Features.Resources.Firesales.Services;
using Quartz;

namespace EliteAPI.Background.Resources;

public class FiresalesUpdateJob(SkyblockFiresalesIngestionService firesalesIngestion) : IJob
{
	public static readonly JobKey Key = new(nameof(FiresalesUpdateJob));

	public async Task Execute(IJobExecutionContext context) {
		await firesalesIngestion.IngestItemsDataAsync();
	}
}