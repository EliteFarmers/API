using System.Diagnostics.Metrics;
using StackExchange.Redis;

namespace EliteAPI.Features.Leaderboards.Services;

public class LeaderboardRedisMemoryMetricsService
{
	private const int MemoryUsageBatchSize = 256;

	private readonly IConnectionMultiplexer _redis;
	private readonly ILogger<LeaderboardRedisMemoryMetricsService> _logger;

	private long _sortedSetBytes;
	private long _memberHashBytes;
	private long _minScoreBytes;
	private long _totalBytes;
	private long _sortedSetKeys;
	private long _memberHashKeys;
	private long _minScoreKeys;
	private long _lastRefreshUnixSeconds;
	private int _refreshSuccessful;

	public LeaderboardRedisMemoryMetricsService(
		IConnectionMultiplexer redis,
		IMeterFactory meterFactory,
		ILogger<LeaderboardRedisMemoryMetricsService> logger) {
		_redis = redis;
		_logger = logger;

		var meter = meterFactory.Create("eliteapi.leaderboard");
		meter.CreateObservableGauge(
			"eliteapi.leaderboard.redis_memory_bytes",
			ObserveRedisMemoryBytes,
			unit: "By",
			description: "Redis memory used by leaderboard keys");
		meter.CreateObservableGauge(
			"eliteapi.leaderboard.redis_keys",
			ObserveRedisKeyCounts,
			description: "Redis key counts used by leaderboard keys");
		meter.CreateObservableGauge(
			"eliteapi.leaderboard.redis_memory_last_refresh_unix",
			() => Interlocked.Read(ref _lastRefreshUnixSeconds),
			unit: "s",
			description: "Unix timestamp of the last successful leaderboard Redis memory sample");
		meter.CreateObservableGauge(
			"eliteapi.leaderboard.redis_memory_refresh_success",
			() => Interlocked.CompareExchange(ref _refreshSuccessful, 0, 0),
			description: "1 when the last leaderboard Redis memory sample succeeded, 0 otherwise");
	}

	private IEnumerable<Measurement<long>> ObserveRedisMemoryBytes() {
		yield return new Measurement<long>(
			Interlocked.Read(ref _totalBytes),
			new KeyValuePair<string, object?>("component", "total"));
		yield return new Measurement<long>(
			Interlocked.Read(ref _sortedSetBytes),
			new KeyValuePair<string, object?>("component", "sorted_set"));
		yield return new Measurement<long>(
			Interlocked.Read(ref _memberHashBytes),
			new KeyValuePair<string, object?>("component", "member_hash"));
		yield return new Measurement<long>(
			Interlocked.Read(ref _minScoreBytes),
			new KeyValuePair<string, object?>("component", "min_score"));
	}

	private IEnumerable<Measurement<long>> ObserveRedisKeyCounts() {
		yield return new Measurement<long>(
			Interlocked.Read(ref _sortedSetKeys) + Interlocked.Read(ref _memberHashKeys) +
			Interlocked.Read(ref _minScoreKeys),
			new KeyValuePair<string, object?>("component", "total"));
		yield return new Measurement<long>(
			Interlocked.Read(ref _sortedSetKeys),
			new KeyValuePair<string, object?>("component", "sorted_set"));
		yield return new Measurement<long>(
			Interlocked.Read(ref _memberHashKeys),
			new KeyValuePair<string, object?>("component", "member_hash"));
		yield return new Measurement<long>(
			Interlocked.Read(ref _minScoreKeys),
			new KeyValuePair<string, object?>("component", "min_score"));
	}

	public async Task RefreshAsync(CancellationToken ct) {
		try {
			var endPoint = _redis.GetEndPoints().FirstOrDefault();
			if (endPoint is null) {
				Interlocked.Exchange(ref _refreshSuccessful, 0);
				return;
			}

			var server = _redis.GetServer(endPoint);
			if (!server.IsConnected) {
				Interlocked.Exchange(ref _refreshSuccessful, 0);
				return;
			}

			var db = _redis.GetDatabase();
			var dbIndex = db.Database >= 0 ? db.Database : 0;

			var (sortedSetBytes, sortedSetKeys) = await SumMemoryUsageAsync(
				server.Keys(dbIndex, "lb:*", pageSize: 1000),
				db,
				key => !key.EndsWith(":temp", StringComparison.Ordinal),
				ct);
			var (memberHashBytes, memberHashKeys) = await SumMemoryUsageAsync(
				server.Keys(dbIndex, "member:*", pageSize: 1000),
				db,
				_ => true,
				ct);
			var (minScoreBytes, minScoreKeys) = await SumMemoryUsageAsync(
				server.Keys(dbIndex, "lb-min:*", pageSize: 1000),
				db,
				_ => true,
				ct);

			var totalBytes = sortedSetBytes + memberHashBytes + minScoreBytes;
			Interlocked.Exchange(ref _sortedSetBytes, sortedSetBytes);
			Interlocked.Exchange(ref _memberHashBytes, memberHashBytes);
			Interlocked.Exchange(ref _minScoreBytes, minScoreBytes);
			Interlocked.Exchange(ref _totalBytes, totalBytes);
			Interlocked.Exchange(ref _sortedSetKeys, sortedSetKeys);
			Interlocked.Exchange(ref _memberHashKeys, memberHashKeys);
			Interlocked.Exchange(ref _minScoreKeys, minScoreKeys);
			Interlocked.Exchange(ref _lastRefreshUnixSeconds, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
			Interlocked.Exchange(ref _refreshSuccessful, 1);
		}
		catch (OperationCanceledException) when (ct.IsCancellationRequested) {
			// Graceful shutdown
		}
		catch (RedisServerException ex) {
			Interlocked.Exchange(ref _refreshSuccessful, 0);
			_logger.LogWarning(ex,
				"Failed to sample leaderboard Redis memory. Ensure the MEMORY command is enabled on Redis.");
		}
		catch (Exception ex) {
			Interlocked.Exchange(ref _refreshSuccessful, 0);
			_logger.LogWarning(ex, "Failed to sample leaderboard Redis memory usage.");
		}
	}

	private static async Task<(long totalBytes, long keyCount)> SumMemoryUsageAsync(
		IEnumerable<RedisKey> keys,
		IDatabase db,
		Func<string, bool> includePredicate,
		CancellationToken ct) {
		long totalBytes = 0;
		long keyCount = 0;
		var tasks = new List<Task<RedisResult>>(MemoryUsageBatchSize);

		foreach (var key in keys) {
			ct.ThrowIfCancellationRequested();
			var keyString = key.ToString();
			if (!includePredicate(keyString)) continue;

			tasks.Add(db.ExecuteAsync("MEMORY", "USAGE", key));
			if (tasks.Count < MemoryUsageBatchSize) continue;

			var (chunkBytes, chunkCount) = await DrainBatchAsync(tasks);
			totalBytes += chunkBytes;
			keyCount += chunkCount;
		}

		if (tasks.Count > 0) {
			var (chunkBytes, chunkCount) = await DrainBatchAsync(tasks);
			totalBytes += chunkBytes;
			keyCount += chunkCount;
		}

		return (totalBytes, keyCount);
	}

	private static async Task<(long totalBytes, long keyCount)> DrainBatchAsync(List<Task<RedisResult>> tasks) {
		var results = await Task.WhenAll(tasks);
		tasks.Clear();

		long totalBytes = 0;
		long keyCount = 0;
		foreach (var result in results) {
			if (result.IsNull) continue;
			if (!long.TryParse(result.ToString(), out var bytes) || bytes < 0) continue;
			totalBytes += bytes;
			keyCount++;
		}

		return (totalBytes, keyCount);
	}
}
