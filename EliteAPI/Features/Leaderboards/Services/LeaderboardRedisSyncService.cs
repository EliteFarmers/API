using EliteAPI.Configuration.Settings;
using EliteAPI.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace EliteAPI.Features.Leaderboards.Services;

public class LeaderboardRedisSyncService(
	IServiceProvider serviceProvider,
	IConnectionMultiplexer redis,
	LeaderboardRedisMemoryMetricsService memoryMetrics,
	ILogger<LeaderboardRedisSyncService> logger,
	IOptions<ConfigLeaderboardSettings> settings) : BackgroundService
{
	private readonly ConfigLeaderboardSettings _settings = settings.Value;
	private static readonly string[] CachedModes = ["all", "classic", "ironman", "island"];
	private static readonly TimeSpan RedisCacheTtl = TimeSpan.FromHours(8);
	private const int DefaultCachedRankAmount = 50_000;
	private static readonly RedisValue ProfileHash = "p";
	private static readonly RedisValue IgnHash = "i";
	private static readonly RedisValue UuidHash = "u";
	private readonly SemaphoreSlim _syncLock = new(1, 1);

	public async Task ForceUpdateAsync(CancellationToken ct) {
		await _syncLock.WaitAsync(ct);
		try {
			await UpdateAllLeaderboards(ct);
		}
		finally {
			_syncLock.Release();
		}
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
		logger.LogInformation("Leaderboard Redis Sync Service starting (Full Refresh Mode).");

		// Wait a bit for startup
		await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

		await _syncLock.WaitAsync(stoppingToken);
		try {
			await UpdateAllLeaderboards(stoppingToken);
		}
		finally {
			_syncLock.Release();
		}

		var interval = _settings.CompleteRefreshInterval > 0
			? TimeSpan.FromMinutes(_settings.CompleteRefreshInterval)
			: TimeSpan.FromMinutes(5);

		using var timer = new PeriodicTimer(interval);

		while (await timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested) {
			try {
				await _syncLock.WaitAsync(stoppingToken);
				try {
					await UpdateAllLeaderboards(stoppingToken);
				}
				finally {
					_syncLock.Release();
				}
			}
			catch (Exception ex) {
				logger.LogError(ex, "Error updating leaderboards in background");
			}
		}
	}

	private async Task UpdateAllLeaderboards(CancellationToken ct) {
		var db = redis.GetDatabase();

		using var scope = serviceProvider.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<DataContext>();
		var registrationService = scope.ServiceProvider.GetRequiredService<ILeaderboardRegistrationService>();

		var leaderboardsBySlug = await context.Leaderboards
			.AsNoTracking()
			.ToDictionaryAsync(lb => lb.Slug, ct);

		logger.LogInformation("Syncing {Count} leaderboard variants to Redis...",
			registrationService.LeaderboardsById.Count * CachedModes.Length);

		foreach (var (slug, definition) in registrationService.LeaderboardsById.OrderBy(x => x.Key)) {
			if (ct.IsCancellationRequested) break;
			if (!leaderboardsBySlug.TryGetValue(slug, out var leaderboard)) continue;

			var intervalType = LbService.GetTypeFromSlug(slug);
			var intervalIdentifier = LbService.GetCurrentIdentifier(intervalType);
			var cacheAmount = definition.Info.CachedRankAmount > 0
				? definition.Info.CachedRankAmount
				: DefaultCachedRankAmount;

			foreach (var mode in CachedModes) {
				var gameMode = mode == "all" ? null : mode;

				try {
					var query = context.LeaderboardEntries.AsNoTracking()
						.Where(e => e.LeaderboardId == leaderboard.LeaderboardId
						            && !e.IsRemoved
						            && e.IntervalIdentifier == intervalIdentifier);

					if (gameMode is not null) {
						query = query.Where(e => e.ProfileType == gameMode);
					}

					var entries = await query
						.OrderByDescending(e => e.Score)
						.Take(cacheAmount)
						.Select(e => new {
							e.ProfileMemberId,
							e.ProfileId,
							e.Score,
							ProfileMember = e.ProfileMember == null
								? null
								: new {
									MinecraftAccount = new { Name = e.ProfileMember.MinecraftAccount.Name },
									e.ProfileMember.PlayerUuid,
									e.ProfileMember.ProfileName,
									Profile = new {
										ProfileName = e.ProfileMember.Profile.ProfileName,
										e.ProfileMember.Profile.ProfileId
									}
								},
							Profile = e.Profile == null
								? null
								: new {
									e.Profile.ProfileName,
									e.Profile.ProfileId
								}
						})
						.ToListAsync(ct);

					var lbKey = $"lb:{slug}:{mode}";
					var tempKey = $"lb:{slug}:{mode}:temp";
					var minKey = $"lb-min:{slug}:{mode}";
					var sortedSetEntries = entries.Select(e => new SortedSetEntry(
						e.ProfileMemberId?.ToString() ?? e.ProfileId,
						(double)e.Score
					)).ToArray();

					if (sortedSetEntries.Length == 0) {
						await db.KeyDeleteAsync([lbKey, tempKey, minKey]);
						continue;
					}

					var transaction = db.CreateTransaction();
					_ = transaction.KeyDeleteAsync(tempKey);
					_ = transaction.SortedSetAddAsync(tempKey, sortedSetEntries);
					_ = transaction.KeyRenameAsync(tempKey, lbKey, When.Always);
					_ = transaction.KeyExpireAsync(lbKey, RedisCacheTtl);
					_ = transaction.StringSetAsync(minKey, sortedSetEntries[^1].Score);
					_ = transaction.KeyExpireAsync(minKey, RedisCacheTtl);
					await transaction.ExecuteAsync();

					var batch = db.CreateBatch();
					foreach (var e in entries) {
						var memberId = e.ProfileMemberId?.ToString() ?? e.ProfileId;
						var memberKey = $"member:{memberId}";

						string ign = "", uuid = "", profileName = "";
						if (e.ProfileMember != null) {
							ign = e.ProfileMember.MinecraftAccount.Name;
							uuid = e.ProfileMember.PlayerUuid;
							profileName = e.ProfileMember.ProfileName ?? e.ProfileMember.Profile.ProfileName;

							var memberIndexKey = $"memberid:{e.ProfileMember.Profile.ProfileId}:{uuid}";
							_ = batch.StringSetAsync(memberIndexKey, memberId, RedisCacheTtl);
						}
						else if (e.Profile != null) {
							profileName = e.Profile.ProfileName;
							uuid = e.Profile.ProfileId;
						}

						_ = batch.HashSetAsync(memberKey, [
							new HashEntry(ProfileHash, profileName),
							new HashEntry(IgnHash, ign),
							new HashEntry(UuidHash, uuid)
						]);
						_ = batch.KeyExpireAsync(memberKey, RedisCacheTtl);
					}

					batch.Execute();

					logger.LogInformation(
						"Synced {Slug} ({Mode}, Interval={Interval}) to Redis ({Count} entries, top {Cached}).",
						slug, mode, intervalIdentifier ?? "current", entries.Count, cacheAmount);
				}
				catch (Exception ex) {
					logger.LogError(ex, "Failed syncing leaderboard {Slug}:{Mode}", slug, mode);
				}
			}
		}

		// Refresh Redis leaderboard memory gauges right after rebuild work.
		await memoryMetrics.RefreshAsync(ct);
	}
}