using EliteAPI.Features.Resources.Items.Services;
using Quartz;

namespace EliteAPI.Background.Resources;

public class ItemsUpdateJob(SkyblockItemsIngestionService itemsIngestion) : IJob {
	public static readonly JobKey Key = new(nameof(ItemsUpdateJob));

	public async Task Execute(IJobExecutionContext context) {
		await itemsIngestion.IngestItemsDataAsync();
	}
}