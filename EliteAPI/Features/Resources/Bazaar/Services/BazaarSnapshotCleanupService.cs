using EliteAPI.Configuration.Settings;
using EliteAPI.Data;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EliteAPI.Features.Resources.Bazaar;

[RegisterService<BazaarSnapshotCleanupService>(LifeTime.Scoped)]
public class BazaarSnapshotCleanupService(
	DataContext context,
	IOptions<ConfigBazaarSnapshotSettings> settings,
	TimeProvider timeProvider,
	ILogger<BazaarSnapshotCleanupService> logger)
{
	private readonly ConfigBazaarSnapshotSettings _settings = settings.Value;

	public async Task<int> DownsampleOldSnapshotsAsync(CancellationToken cancellationToken = default) {
		if (!_settings.CleanupEnabled) {
			logger.LogDebug("Bazaar snapshot cleanup is disabled");
			return 0;
		}

		var downsampleAfterDays = Math.Max(1, _settings.DownsampleAfterDays);
		var downsampleBucketMinutes = Math.Max(1, _settings.DownsampleBucketMinutes);
		var bucketSeconds = downsampleBucketMinutes * 60;
		var cutoff = timeProvider.GetUtcNow().AddDays(-downsampleAfterDays);

		// Keep only the newest snapshot for each product within each time bucket once data ages past the cutoff.
		var deletedCount = await context.Database.ExecuteSqlInterpolatedAsync($"""
			WITH ranked_snapshots AS (
				SELECT
					"Id",
					ROW_NUMBER() OVER (
						PARTITION BY
							"ProductId",
							CAST(FLOOR(EXTRACT(EPOCH FROM "RecordedAt") / {bucketSeconds}) AS bigint)
						ORDER BY "RecordedAt" DESC, "Id" DESC
					) AS row_number
				FROM "BazaarProductSnapshots"
				WHERE "RecordedAt" < {cutoff}
			)
			DELETE FROM "BazaarProductSnapshots" AS snapshot
			USING ranked_snapshots
			WHERE snapshot."Id" = ranked_snapshots."Id"
			  AND ranked_snapshots.row_number > 1
			""", cancellationToken);

		logger.LogInformation(
			"Downsampled {DeletedCount} bazaar snapshots older than {Cutoff} using {BucketMinutes}-minute buckets",
			deletedCount, cutoff, downsampleBucketMinutes);

		return deletedCount;
	}
}
