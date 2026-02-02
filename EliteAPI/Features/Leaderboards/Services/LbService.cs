using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using EFCore.BulkExtensions;
using EliteAPI.Data;
using EliteAPI.Features.Leaderboards.Models;
using EliteAPI.Features.Profiles.Services;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Utilities;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using StackExchange.Redis;

namespace EliteAPI.Features.Leaderboards.Services;

public interface ILbService
{
	Task<(Leaderboard? lb, ILeaderboardDefinition? definition)> GetLeaderboard(string leaderboardId);

	Task<List<LeaderboardEntryDto>> GetLeaderboardSlice(string leaderboardId, int offset = 0, int limit = 20,
		string? gameMode = null, RemovedFilter removedFilter = RemovedFilter.NotRemoved, string? identifier = null);

	Task<LeaderboardEntryWithRankDto?> GetLastLeaderboardEntry(string leaderboardId, string? gameMode = null,
		RemovedFilter removedFilter = RemovedFilter.NotRemoved, string? identifier = null);

	Task<string?> GetFirstInterval(string leaderboardId);
	double GetLeaderboardMinScore(string leaderboardId);
	Task UpdateMemberLeaderboardsAsync(ProfileMember member, CancellationToken c);
	Task<List<LeaderboardUpdateEntry>> GetLeaderboardUpdatesAsync(ProfileMember member, CancellationToken c);
	Task<List<LeaderboardUpdateEntry>> GetProfileLeaderboardUpdatesAsync(Profile profile, CancellationToken c);
	Task ProcessLeaderboardUpdatesAsync(List<LeaderboardUpdateEntry> updates, CancellationToken c);
	Task EnsureMemberIntervalEntriesExist(Guid profileMemberId, CancellationToken c = default);
	Task UpdateProfileLeaderboardsAsync(Profile profile, CancellationToken c);
	Task<List<PlayerLeaderboardEntryWithRankDto>> GetPlayerLeaderboardEntriesWithRankAsync(Guid profileMemberId);
	Task<List<PlayerLeaderboardEntryWithRankDto>> GetProfileLeaderboardEntriesWithRankAsync(string profileId);

	Task<PlayerLeaderboardEntryWithRankDto?> GetLeaderboardEntryAsync(string leaderboardSlug, string memberOrProfileId,
		string? gameMode = null, RemovedFilter removedFilter = RemovedFilter.NotRemoved, string? identifier = null);

	Task<Dictionary<string, LeaderboardPositionDto?>> GetMultipleLeaderboardRanks(
		List<string> leaderboards, string playerUuid, string profileId, int? upcoming = null, int? previous = null,
		int? atRank = null, string? gameMode = null, RemovedFilter removedFilter = RemovedFilter.NotRemoved,
		string? identifier = null, CancellationToken? c = null);

	Task<LeaderboardPositionDto> GetLeaderboardRank(string leaderboardId, string playerUuid, string profileId,
		int? upcoming = null, int? previous = null, int? atRank = null, double? atAmount = null,
		string? gameMode = null,
		RemovedFilter removedFilter = RemovedFilter.NotRemoved, string? identifier = null, bool skipUpdate = false,
		CancellationToken? c = null);

	Task<LeaderboardPositionDto?> GetLeaderboardRankByResourceId(string leaderboardId, string resourceId,
		int? upcoming = null, int? previous = null, int? atRank = null, double? atAmount = null,
		string? gameMode = null,
		RemovedFilter removedFilter = RemovedFilter.NotRemoved, string? identifier = null, bool skipUpdate = false,
		CancellationToken? c = null);

	(long start, long end) GetCurrentTimeRange(LeaderboardType type);
	(long start, long end) GetIntervalTimeRange(LeaderboardType type, DateTimeOffset now);
	(long start, long end) GetIntervalTimeRange(string? interval);

	Task<List<LeaderboardEntryDto>> GetGuildMembersLeaderboardEntriesAsync(string guildId, string leaderboardSlug,
		string? identifier = null, string? gameMode = null);
}

[RegisterService<ILbService>(LifeTime.Scoped)]
public class LbService(
	ILeaderboardRegistrationService registrationService,
	ILogger<LbService> logger,
	IConnectionMultiplexer redis,
	HybridCache cache,
	ILeaderboardCacheMetrics cacheMetrics,
	IMemberService memberService,
	DataContext context)
	: ILbService
{
	/// <summary>
	/// Cached anchor data for leaderboard rank lookups
	/// </summary>
	private record CachedAnchor(decimal Score, int EntryId);

	/// <summary>
	/// Cached anchor data for score-based lookups (stores rank at a score threshold)
	/// </summary>
	private record CachedAmountAnchor(int Rank);

	/// <summary>
	/// Get the bucket size for a given rank. Higher ranks get finer granularity.
	/// Buckets must be larger than typical upcoming request sizes (100) to have any chance of cache hits.
	/// </summary>
	private static int GetRankBucket(int atRank) {
		return atRank switch {
			<= 1000 => 10, // Top 1k: bucket of 10, 100 possible buckets
			<= 5000 => 50, // Top 5k: bucket of 50, 80 possible buckets  
			<= 25000 => 250, // Top 25k: bucket of 250, 80 possible buckets
			<= 50000 => 500, // Top 50k: bucket of 500, 50 possible buckets
			<= 100000 => 1000, // Top 100k: bucket of 1000, 50 possible buckets
			_ => 2500 // 100k+: bucket of 2500
		};
	}

	/// <summary>
	/// Round a rank to its bucket boundary
	/// </summary>
	private static int GetBucketedRank(int atRank) {
		var bucket = GetRankBucket(atRank);
		if (bucket == 1) return atRank;
		return (int)Math.Ceiling((double)atRank / bucket) * bucket;
	}

	/// <summary>
	/// Get the cache TTL for a given rank. Higher ranks get shorter TTLs for fresher data.
	/// </summary>
	private static HybridCacheEntryOptions GetCacheOptions(int atRank) {
		var ttl = atRank switch {
			<= 1000 => TimeSpan.FromSeconds(20),
			<= 5000 => TimeSpan.FromSeconds(30),
			<= 25000 => TimeSpan.FromSeconds(45),
			<= 50000 => TimeSpan.FromSeconds(60),
			_ => TimeSpan.FromSeconds(120)
		};
		return new HybridCacheEntryOptions {
			Expiration = ttl,
			LocalCacheExpiration = TimeSpan.FromSeconds(Math.Min(ttl.TotalSeconds, 15))
		};
	}

	/// <summary>
	/// Round upcoming count to standard bucket sizes for better cache hit rates.
	/// </summary>
	private static int GetBucketedUpcoming(int upcoming) {
		// Maximize cache reuse, a switch statmement might be used later
		return 100;
	}

	public async Task<(Leaderboard? lb, ILeaderboardDefinition? definition)> GetLeaderboard(string leaderboardId) {
		if (!registrationService.LeaderboardsById.TryGetValue(leaderboardId, out var definition)) return (null, null);

		var lb = await context.Leaderboards
			.AsNoTracking()
			.FirstOrDefaultAsync(lb => lb.Slug == leaderboardId);

		return (lb, definition);
	}

	public async Task<List<LeaderboardEntryDto>> GetLeaderboardSlice(string leaderboardId, int offset = 0,
		int limit = 20, string? gameMode = null, RemovedFilter removedFilter = RemovedFilter.NotRemoved,
		string? identifier = null) {
		var (lb, definition) = await GetLeaderboard(leaderboardId);
		if (lb is null || definition is null) return [];

		if (definition is IMemberLeaderboardDefinition)
			return await context.LeaderboardEntries.AsNoTracking()
				.FromLeaderboard(lb.LeaderboardId, true)
				.EntryFilter(gameMode: gameMode, removedFilter: removedFilter, interval: identifier)
				.OrderByDescending(e => e.Score)
				.ThenByDescending(e => e.LeaderboardEntryId)
				.Skip(offset)
				.Take(limit)
				.MapToMemberLeaderboardEntries(limit <= 20)
				.ToListAsync();

		if (definition is IProfileLeaderboardDefinition)
			return await context.LeaderboardEntries.AsNoTracking()
				.FromLeaderboard(lb.LeaderboardId, false)
				.EntryFilter(gameMode: gameMode, removedFilter: removedFilter, interval: identifier)
				.OrderByDescending(e => e.Score)
				.ThenByDescending(e => e.LeaderboardEntryId)
				.Skip(offset)
				.Take(limit)
				.MapToProfileLeaderboardEntries(removedFilter)
				.ToListAsync();

		return [];
	}

	public async Task<LeaderboardEntryWithRankDto?> GetLastLeaderboardEntry(string leaderboardId,
		string? gameMode = null, RemovedFilter removedFilter = RemovedFilter.NotRemoved, string? identifier = null) {
		var (lb, definition) = await GetLeaderboard(leaderboardId);
		if (lb is null || definition is null) return null;

		var key = $"leaderboard-max:{leaderboardId}:{gameMode ?? "all"}:{identifier ?? "current"}:{removedFilter}";
		var db = redis.GetDatabase();
		if (await db.KeyExistsAsync(key)) {
			var uuid = await db.StringGetAsync(key);
			if (uuid.HasValue) return JsonSerializer.Deserialize<LeaderboardEntryWithRankDto>(uuid.ToString());
		}

		LeaderboardEntryWithRankDto? entry = null;
		var rank = -1;

		if (definition is IMemberLeaderboardDefinition) {
			entry = await context.LeaderboardEntries.AsNoTracking()
				.FromLeaderboard(lb.LeaderboardId, true)
				.EntryFilter(gameMode: gameMode, removedFilter: removedFilter, interval: identifier)
				.OrderBy(e => e.Score)
				.Select(e => new LeaderboardEntryWithRankDto {
					Uuid = e.ProfileMember!.PlayerUuid,
					Profile = e.ProfileMember.ProfileName,
					Amount = (double)e.Score,
					InitialAmount = (double)e.InitialScore,
					Mode = e.ProfileType,
					Removed = e.IsRemoved,
					Ign = e.ProfileMember.MinecraftAccount.Name
				}).FirstOrDefaultAsync();

			rank = await context.LeaderboardEntries
				.FromLeaderboard(lb.LeaderboardId, true)
				.EntryFilter(gameMode: gameMode, removedFilter: removedFilter, interval: identifier)
				.CountAsync() + 1;
		}

		if (definition is IProfileLeaderboardDefinition) {
			entry = await context.LeaderboardEntries.AsNoTracking()
				.FromLeaderboard(lb.LeaderboardId, false)
				.EntryFilter(gameMode: gameMode, removedFilter: removedFilter, interval: identifier)
				.Include(e => e.Profile)
				.OrderBy(e => e.Score)
				.Select(e => new LeaderboardEntryWithRankDto {
					Uuid = e.Profile!.ProfileId,
					Profile = e.Profile!.ProfileName,
					Amount = (double)e.Score,
					InitialAmount = (double)e.InitialScore,
					Mode = e.ProfileType,
					Removed = e.IsRemoved,
					Members = e.Profile.Members
						.Where(m =>
							(removedFilter == RemovedFilter.NotRemoved && m.WasRemoved == false)
							|| (removedFilter == RemovedFilter.Removed && m.WasRemoved == true)
							|| removedFilter == RemovedFilter.All)
						.Select(m => new ProfileLeaderboardMemberDto {
							Ign = m.MinecraftAccount.Name,
							Uuid = m.PlayerUuid,
							Xp = m.SkyblockXp,
							Removed = m.WasRemoved
						}).OrderByDescending(s => s.Xp).ToList()
				}).FirstOrDefaultAsync();

			rank = await context.LeaderboardEntries
				.FromLeaderboard(lb.LeaderboardId, false)
				.EntryFilter(gameMode: gameMode, removedFilter: removedFilter, interval: identifier)
				.CountAsync() + 1;
		}

		if (entry is not null) {
			entry.Rank = rank;

			if (rank != -1) await db.StringSetAsync(key, JsonSerializer.Serialize(entry), TimeSpan.FromMinutes(1));

			return entry;
		}

		return null;
	}

	/// <summary>
	/// Get the first recorded interval for a leaderboard, used for frontends to determine the first interval to allow a user to select.
	/// </summary>
	/// <param name="leaderboardId"></param>
	/// <returns></returns>
	public async Task<string?> GetFirstInterval(string leaderboardId) {
		if (!registrationService.LeaderboardsById.TryGetValue(leaderboardId, out var definition)) return null;

		// Check redis cache first
		var db = redis.GetDatabase();
		var cacheKey = $"leaderboard-first-interval:{leaderboardId}";
		if (await db.KeyExistsAsync(cacheKey)) {
			var cachedValue = await db.StringGetAsync(cacheKey);
			if (cachedValue.HasValue) return cachedValue.ToString();
		}

		var lb = await context.Leaderboards
			.AsNoTracking()
			.FirstOrDefaultAsync(lb => lb.Slug == leaderboardId);
		if (lb is null) return null;

		var firstEntry = await context.LeaderboardEntries
			.AsNoTracking()
			.Where(e => e.LeaderboardId == lb.LeaderboardId)
			.OrderBy(e => e.IntervalIdentifier)
			.Select(e => e.IntervalIdentifier)
			.FirstOrDefaultAsync();

		if (firstEntry is null) return null;

		// Store the first interval in the cache for 1 hour
		await db.StringSetAsync(cacheKey, firstEntry, TimeSpan.FromHours(3));

		return firstEntry;
	}

	public double GetLeaderboardMinScore(string leaderboardId) {
		if (!registrationService.LeaderboardsById.TryGetValue(leaderboardId, out var definition)) return 0;
		return (double)definition.Info.MinimumScore;
	}

	public async Task UpdateMemberLeaderboardsAsync(ProfileMember member, CancellationToken c) {
		if (member.Profile?.GameMode == "bingo") return;
		var time = DateTime.UtcNow;

		var monthlyInterval = GetCurrentIdentifier(LeaderboardType.Monthly);
		var weeklyInterval = GetCurrentIdentifier(LeaderboardType.Weekly);

		var leaderboardsIdsBySlug = await context.Leaderboards
			.AsNoTracking()
			.Select(lb => new { lb.Slug, lb.LeaderboardId, lb.MinimumScore })
			.ToDictionaryAsync(lb => lb.Slug, c);

		var existingEntryList = await context.LeaderboardEntries
			.AsNoTracking()
			.Where(e =>
				e.ProfileMemberId == member.Id
				&& (e.IntervalIdentifier == monthlyInterval || e.IntervalIdentifier == weeklyInterval ||
				    e.IntervalIdentifier == null))
			.ToListAsync(c);

		var existingEntries = new Dictionary<int, Models.LeaderboardEntry>();
		List<Models.LeaderboardEntry>? failed = null;

		// Add existing entries to a dictionary to check for duplicates
		foreach (var entry in existingEntryList.Where(entry => !existingEntries.TryAdd(entry.LeaderboardId, entry))) {
			failed ??= [];
			failed.Add(entry);
		}

		// Delete duplicate entries (this should be very rare, but less expensive than a unique db constraint)
		if (failed is { Count: > 0 }) {
			await context.BulkDeleteAsync(failed, cancellationToken: c);
			logger.LogWarning("Deleted {Count} duplicate leaderboard entries for {Player}", failed.Count,
				member.PlayerUuid);
		}

		var updatedEntries = new List<Models.LeaderboardEntry>();
		var newEntries = new List<Models.LeaderboardEntry>();
		var ranRestore = false;

		foreach (var (slug, definition) in registrationService.LeaderboardsById) {
			if (definition is not IMemberLeaderboardDefinition memberLb) continue;

			var useIncrease = definition.Info.UseIncreaseForInterval;
			if (!leaderboardsIdsBySlug.TryGetValue(slug, out var lb)) continue;

			var type = GetTypeFromSlug(slug);
			var intervalIdentifier = type switch {
				LeaderboardType.Monthly => monthlyInterval,
				LeaderboardType.Weekly => weeklyInterval,
				_ => null
			};

			var score = memberLb.GetScoreFromMember(member, type);

			if (existingEntries.TryGetValue(lb.LeaderboardId, out var entry)) {
				var changed = false;

				if (member.Profile?.GameMode != entry.ProfileType) {
					entry.ProfileType = member.Profile?.GameMode;
					changed = true;
				}

				if (entry.IsRemoved != member.WasRemoved) {
					entry.IsRemoved = member.WasRemoved;
					changed = true;

					if (entry.IsRemoved == false && !ranRestore) {
						await RestoreMemberLeaderboards(member.Id, c);
						ranRestore = true;
					}
				}

				if (score >= 0 && (score >= entry.InitialScore || !useIncrease)) {
					var newScore = entry.IntervalIdentifier is not null && useIncrease
						? score - entry.InitialScore
						: score;

					if (entry.Score != newScore) {
						entry.Score = newScore;
						changed = true;
					}
				}

				if (changed) updatedEntries.Add(entry);

				continue;
			}

			if (score <= 0 || score < lb.MinimumScore) continue;

			var newEntry = new Models.LeaderboardEntry {
				LeaderboardId = lb.LeaderboardId,
				IntervalIdentifier = intervalIdentifier,

				ProfileMemberId = member.Id,

				InitialScore = useIncrease && intervalIdentifier is not null ? score : 0,
				Score = useIncrease && intervalIdentifier is not null ? 0 : score,

				IsRemoved = member.WasRemoved,
				ProfileType = member.Profile?.GameMode
			};

			newEntries.Add(newEntry);
		}

		if (updatedEntries.Count != 0) {
			var options = new BulkConfig {
				PropertiesToIncludeOnUpdate = [
					nameof(Models.LeaderboardEntry.Score),
					nameof(Models.LeaderboardEntry.IsRemoved),
					nameof(Models.LeaderboardEntry.ProfileType)
				]
			};
			await context.BulkUpdateAsync(updatedEntries, options, cancellationToken: c);
			logger.LogInformation("Updated {Count} leaderboard entries", updatedEntries.Count);
		}

		if (newEntries.Count != 0) {
			var options = new BulkConfig { SetOutputIdentity = false };
			await context.BulkInsertAsync(newEntries, options, cancellationToken: c);
			logger.LogInformation("Inserted {Count} new leaderboard entries", newEntries.Count);
		}

		logger.LogInformation("Updating member leaderboards for {Player} on {Profile} took {Time}ms",
			member.PlayerUuid,
			member.Profile?.ProfileId,
			DateTime.UtcNow.Subtract(time).TotalMilliseconds.ToString(CultureInfo.InvariantCulture)
		);
	}

	public async Task<List<LeaderboardUpdateEntry>> GetLeaderboardUpdatesAsync(ProfileMember member,
		CancellationToken c) {
		if (member.Profile?.GameMode == "bingo") return [];

		var monthlyInterval = GetCurrentIdentifier(LeaderboardType.Monthly);
		var weeklyInterval = GetCurrentIdentifier(LeaderboardType.Weekly);

		var leaderboardsIdsBySlug = await context.Leaderboards
			.AsNoTracking()
			.Select(lb => new { lb.Slug, lb.LeaderboardId, lb.MinimumScore })
			.ToDictionaryAsync(lb => lb.Slug, c);

		var existingEntryList = await context.LeaderboardEntries
			.AsNoTracking()
			.Where(e =>
				e.ProfileMemberId == member.Id
				&& (e.IntervalIdentifier == monthlyInterval || e.IntervalIdentifier == weeklyInterval ||
				    e.IntervalIdentifier == null))
			.ToListAsync(c);

		var existingEntries = new Dictionary<int, Models.LeaderboardEntry>();
		var updates = new List<LeaderboardUpdateEntry>();

		// Add existing entries to a dictionary, mark duplicates for deletion
		foreach (var entry in existingEntryList) {
			if (!existingEntries.TryAdd(entry.LeaderboardId, entry)) {
				// Duplicate entry - mark for deletion
				updates.Add(new LeaderboardUpdateEntry {
					LeaderboardId = entry.LeaderboardId,
					ProfileMemberId = entry.ProfileMemberId,
					Score = entry.Score,
					InitialScore = entry.InitialScore,
					IntervalIdentifier = entry.IntervalIdentifier,
					IsRemoved = entry.IsRemoved,
					ProfileType = entry.ProfileType,
					Operation = LeaderboardUpdateOperation.Delete,
					ExistingEntryId = entry.LeaderboardEntryId
				});
			}
		}

		foreach (var (slug, definition) in registrationService.LeaderboardsById) {
			if (definition is not IMemberLeaderboardDefinition memberLb) continue;

			var useIncrease = definition.Info.UseIncreaseForInterval;
			if (!leaderboardsIdsBySlug.TryGetValue(slug, out var lb)) continue;

			var type = GetTypeFromSlug(slug);
			var intervalIdentifier = type switch {
				LeaderboardType.Monthly => monthlyInterval,
				LeaderboardType.Weekly => weeklyInterval,
				_ => null
			};

			var score = memberLb.GetScoreFromMember(member, type);

			if (existingEntries.TryGetValue(lb.LeaderboardId, out var entry)) {
				var changed = false;
				var newProfileType = entry.ProfileType;
				var newIsRemoved = entry.IsRemoved;
				var newScore = entry.Score;

				if (member.Profile?.GameMode != entry.ProfileType) {
					newProfileType = member.Profile?.GameMode;
					changed = true;
				}

				if (entry.IsRemoved != member.WasRemoved) {
					newIsRemoved = member.WasRemoved;
					changed = true;
				}

				if (score >= 0 && (score >= entry.InitialScore || !useIncrease)) {
					var calculatedScore = entry.IntervalIdentifier is not null && useIncrease
						? score - entry.InitialScore
						: score;

					if (entry.Score != calculatedScore) {
						newScore = calculatedScore;
						changed = true;
					}
				}

				if (changed) {
					updates.Add(new LeaderboardUpdateEntry {
						LeaderboardId = lb.LeaderboardId,
						ProfileMemberId = member.Id,
						Score = newScore,
						InitialScore = entry.InitialScore,
						IntervalIdentifier = entry.IntervalIdentifier,
						IsRemoved = newIsRemoved,
						ProfileType = newProfileType,
						Operation = LeaderboardUpdateOperation.Update,
						ExistingEntryId = entry.LeaderboardEntryId
					});
				}

				continue;
			}

			if (score <= 0 || score < lb.MinimumScore) continue;

			updates.Add(new LeaderboardUpdateEntry {
				LeaderboardId = lb.LeaderboardId,
				ProfileMemberId = member.Id,
				Score = useIncrease && intervalIdentifier is not null ? 0 : score,
				InitialScore = useIncrease && intervalIdentifier is not null ? score : 0,
				IntervalIdentifier = intervalIdentifier,
				IsRemoved = member.WasRemoved,
				ProfileType = member.Profile?.GameMode,
				Operation = LeaderboardUpdateOperation.Insert
			});
		}

		return updates;
	}

	public async Task ProcessLeaderboardUpdatesAsync(List<LeaderboardUpdateEntry> updates, CancellationToken c) {
		if (updates.Count == 0) return;

		// Group by operation type
		var inserts = updates.Where(u => u.Operation == LeaderboardUpdateOperation.Insert).ToList();
		var updateOps = updates.Where(u => u.Operation == LeaderboardUpdateOperation.Update).ToList();
		var deletes = updates.Where(u => u.Operation == LeaderboardUpdateOperation.Delete).ToList();

		// Handle deletes first (duplicates, etc.)
		if (deletes.Count > 0) {
			var deleteIds = deletes
				.Where(d => d.ExistingEntryId.HasValue)
				.Select(d => d.ExistingEntryId!.Value)
				.ToList();

			if (deleteIds.Count > 0) {
				await context.LeaderboardEntries
					.Where(e => deleteIds.Contains(e.LeaderboardEntryId))
					.ExecuteDeleteAsync(c);
				logger.LogInformation("Deleted {Count} leaderboard entries", deleteIds.Count);
			}
		}

		// Handle updates
		if (updateOps.Count > 0) {
			// Deduplicate - keep only the latest update per entry
			var latestUpdates = updateOps
				.Where(u => u.ExistingEntryId.HasValue)
				.GroupBy(u => u.ExistingEntryId!.Value)
				.Select(g => g.Last())
				.ToList();

			var entriesToUpdate = latestUpdates.Select(u => new Models.LeaderboardEntry {
				LeaderboardEntryId = u.ExistingEntryId!.Value,
				LeaderboardId = u.LeaderboardId,
				ProfileMemberId = u.ProfileMemberId,
				ProfileId = u.ProfileId,
				Score = u.Score,
				InitialScore = u.InitialScore,
				IntervalIdentifier = u.IntervalIdentifier,
				IsRemoved = u.IsRemoved,
				ProfileType = u.ProfileType
			}).ToList();

			var updateConfig = new BulkConfig {
				PropertiesToIncludeOnUpdate = [
					nameof(Models.LeaderboardEntry.Score),
					nameof(Models.LeaderboardEntry.IsRemoved),
					nameof(Models.LeaderboardEntry.ProfileType)
				]
			};
			await context.BulkUpdateAsync(entriesToUpdate, updateConfig, cancellationToken: c);
			logger.LogInformation("Bulk updated {Count} leaderboard entries", entriesToUpdate.Count);
		}

		// Handle inserts
		if (inserts.Count > 0) {
			// Deduplicate - keep only one insert per leaderboard+member/profile combination
			var uniqueInserts = inserts
				.GroupBy(i => (i.LeaderboardId, i.ProfileMemberId, i.ProfileId, i.IntervalIdentifier))
				.Select(g => g.Last())
				.ToList();

			var entriesToInsert = uniqueInserts.Select(u => new Models.LeaderboardEntry {
				LeaderboardId = u.LeaderboardId,
				ProfileMemberId = u.ProfileMemberId,
				ProfileId = u.ProfileId,
				Score = u.Score,
				InitialScore = u.InitialScore,
				IntervalIdentifier = u.IntervalIdentifier,
				IsRemoved = u.IsRemoved,
				ProfileType = u.ProfileType
			}).ToList();

			var insertConfig = new BulkConfig { SetOutputIdentity = false };
			await context.BulkInsertAsync(entriesToInsert, insertConfig, cancellationToken: c);
			logger.LogInformation("Bulk inserted {Count} leaderboard entries", entriesToInsert.Count);
		}
	}

	private async Task RestoreMemberLeaderboards(Guid profileMemberId, CancellationToken c = default) {
		await context.LeaderboardEntries
			.Where(e => e.ProfileMemberId == profileMemberId && e.IsRemoved == true)
			.ExecuteUpdateAsync(s => s.SetProperty(le => le.IsRemoved, false), c);
	}

	public async Task EnsureMemberIntervalEntriesExist(Guid profileMemberId, CancellationToken c = default) {
		var monthlyInterval = GetCurrentIdentifier(LeaderboardType.Monthly);
		var weeklyInterval = GetCurrentIdentifier(LeaderboardType.Weekly);

		// Create missing interval entries for weekly leaderboards
		await CreateMissingIntervalEntries(profileMemberId, weeklyInterval!, LeaderboardType.Weekly, c);
		// Create missing interval entries for monthly leaderboards
		await CreateMissingIntervalEntries(profileMemberId, monthlyInterval!, LeaderboardType.Monthly, c);
	}

	private async Task CreateMissingIntervalEntries(Guid profileMemberId, string intervalIdentifier,
		LeaderboardType type, CancellationToken c) {
		var sql = """
		          INSERT INTO "LeaderboardEntries" (
		          	"LeaderboardId", "IntervalIdentifier", "ProfileMemberId", 
		          	"InitialScore", "Score", "IsRemoved", "ProfileType"
		          )
		          SELECT 
		          	latest."LeaderboardId",
		          	@intervalIdentifier,
		          	latest."ProfileMemberId",
		          	(latest."Score" + latest."InitialScore"),
		          	0,
		          	latest."IsRemoved",
		          	latest."ProfileType"
		          FROM (
		          	SELECT DISTINCT ON ("LeaderboardId") *
		          	FROM "LeaderboardEntries"
		          	WHERE "ProfileMemberId" = @profileMemberId
		          	ORDER BY "LeaderboardId", "IntervalIdentifier" DESC
		          ) latest
		          INNER JOIN "Leaderboards" lb ON lb."LeaderboardId" = latest."LeaderboardId"
		          WHERE lb."IntervalType" = @type
		            AND NOT EXISTS (
		          	  SELECT 1 FROM "LeaderboardEntries" existing
		          	  WHERE existing."ProfileMemberId" = @profileMemberId
		          		AND existing."LeaderboardId" = latest."LeaderboardId"
		          		AND existing."IntervalIdentifier" = @intervalIdentifier
		            )
		          """;

		await context.Database.ExecuteSqlRawAsync(sql,
		[
			new Npgsql.NpgsqlParameter("profileMemberId", profileMemberId),
			new Npgsql.NpgsqlParameter("intervalIdentifier", intervalIdentifier),
			new Npgsql.NpgsqlParameter("type", type.ToString())
		], c);
	}

	public async Task UpdateProfileLeaderboardsAsync(Profile profile,
		CancellationToken c) {
		if (profile.GameMode == "bingo") return;
		var time = DateTime.UtcNow;

		var monthlyInterval = GetCurrentIdentifier(LeaderboardType.Monthly);
		var weeklyInterval = GetCurrentIdentifier(LeaderboardType.Weekly);

		var leaderboardsIdsBySlug = await context.Leaderboards
			.AsNoTracking()
			.Select(lb => new { lb.Slug, lb.LeaderboardId, lb.MinimumScore })
			.ToDictionaryAsync(lb => lb.Slug, c);

		var existingEntryList = await context.LeaderboardEntries
			.AsNoTracking()
			.Where(e =>
				e.ProfileId == profile.ProfileId
				&& (e.IntervalIdentifier == monthlyInterval || e.IntervalIdentifier == weeklyInterval ||
				    e.IntervalIdentifier == null))
			.ToListAsync(c);

		var existingEntries = new Dictionary<int, Models.LeaderboardEntry>();
		List<Models.LeaderboardEntry>? failed = null;

		// Add existing entries to a dictionary to check for duplicates
		foreach (var entry in existingEntryList.Where(entry => !existingEntries.TryAdd(entry.LeaderboardId, entry))) {
			failed ??= [];
			failed.Add(entry);
		}

		// Delete duplicate entries (this should be very rare, but less expensive than a unique db constraint)
		if (failed is { Count: > 0 }) {
			await context.BulkDeleteAsync(failed, cancellationToken: c);
			logger.LogWarning("Deleted {Count} duplicate leaderboard entries for {Profile}", failed.Count,
				profile.ProfileId);
		}

		var updatedEntries = new List<Models.LeaderboardEntry>();
		var newEntries = new List<Models.LeaderboardEntry>();
		var ranRestore = false;

		foreach (var (slug, definition) in registrationService.LeaderboardsById) {
			if (definition is not IProfileLeaderboardDefinition profileLb) continue;

			var useIncrease = definition.Info.UseIncreaseForInterval;
			if (!leaderboardsIdsBySlug.TryGetValue(slug, out var lb)) continue;

			var type = GetTypeFromSlug(slug);
			var intervalIdentifier = type switch {
				LeaderboardType.Monthly => monthlyInterval,
				LeaderboardType.Weekly => weeklyInterval,
				_ => null
			};

			var score = profileLb.GetScoreFromProfile(profile, type);
			if (score == -1 && profile.Garden is not null) {
				if (intervalIdentifier is not null) {
					var valid = IsWithinInterval(type, profile.Garden.LastUpdated);
					if (!valid) continue;
				}

				score = profileLb.GetScoreFromGarden(profile.Garden, type);
			}

			if (existingEntries.TryGetValue(lb.LeaderboardId, out var entry)) {
				var changed = false;

				if (entry.IsRemoved != profile.IsDeleted) {
					entry.IsRemoved = profile.IsDeleted;
					changed = true;

					if (entry.IsRemoved == false && !ranRestore) {
						await RestoreProfileLeaderboards(profile.ProfileId, c);
						ranRestore = true;
					}
				}

				if (profile.GameMode != entry.ProfileType) {
					entry.ProfileType = profile?.GameMode;
					changed = true;
				}

				if (score >= 0 && (score >= entry.InitialScore || !useIncrease)) {
					var newScore = entry.IntervalIdentifier is not null && useIncrease
						? score - entry.InitialScore
						: score;

					if (entry.Score != newScore) {
						entry.Score = newScore;
						changed = true;
					}
				}

				if (changed) updatedEntries.Add(entry);

				continue;
			}

			if (score <= 0 || score < lb.MinimumScore) continue;

			var newEntry = new Models.LeaderboardEntry {
				LeaderboardId = lb.LeaderboardId,
				IntervalIdentifier = intervalIdentifier,

				ProfileId = profile.ProfileId,

				InitialScore = useIncrease && intervalIdentifier is not null ? score : 0,
				Score = useIncrease && intervalIdentifier is not null ? 0 : score,

				IsRemoved = profile.IsDeleted,
				ProfileType = profile.GameMode
			};

			newEntries.Add(newEntry);
		}

		if (updatedEntries.Count != 0) {
			var options = new BulkConfig {
				PropertiesToIncludeOnUpdate = [
					nameof(Models.LeaderboardEntry.Score),
					nameof(Models.LeaderboardEntry.IsRemoved),
					nameof(Models.LeaderboardEntry.ProfileType)
				]
			};
			await context.BulkUpdateAsync(updatedEntries, options, cancellationToken: c);
			logger.LogInformation("Updated {Count} profile leaderboard entries", updatedEntries.Count);
		}

		if (newEntries.Count != 0) {
			var options = new BulkConfig { SetOutputIdentity = false };
			await context.BulkInsertAsync(newEntries, options, cancellationToken: c);
			logger.LogInformation("Inserted {Count} new profile leaderboard entries", newEntries.Count);
		}

		logger.LogInformation("Updating profile leaderboards for {Profile} took {Time}ms",
			profile!.ProfileId,
			DateTime.UtcNow.Subtract(time).TotalMilliseconds.ToString(CultureInfo.InvariantCulture)
		);
	}

	public async Task<List<LeaderboardUpdateEntry>> GetProfileLeaderboardUpdatesAsync(Profile profile,
		CancellationToken c) {
		if (profile.GameMode == "bingo") return [];

		var monthlyInterval = GetCurrentIdentifier(LeaderboardType.Monthly);
		var weeklyInterval = GetCurrentIdentifier(LeaderboardType.Weekly);

		var leaderboardsIdsBySlug = await context.Leaderboards
			.AsNoTracking()
			.Select(lb => new { lb.Slug, lb.LeaderboardId, lb.MinimumScore })
			.ToDictionaryAsync(lb => lb.Slug, c);

		var existingEntryList = await context.LeaderboardEntries
			.AsNoTracking()
			.Where(e =>
				e.ProfileId == profile.ProfileId
				&& (e.IntervalIdentifier == monthlyInterval || e.IntervalIdentifier == weeklyInterval ||
				    e.IntervalIdentifier == null))
			.ToListAsync(c);

		var existingEntries = new Dictionary<int, Models.LeaderboardEntry>();
		var updates = new List<LeaderboardUpdateEntry>();

		// Add existing entries to a dictionary, mark duplicates for deletion
		foreach (var entry in existingEntryList) {
			if (!existingEntries.TryAdd(entry.LeaderboardId, entry)) {
				// Duplicate entry - mark for deletion
				updates.Add(new LeaderboardUpdateEntry {
					LeaderboardId = entry.LeaderboardId,
					ProfileId = entry.ProfileId,
					Score = entry.Score,
					InitialScore = entry.InitialScore,
					IntervalIdentifier = entry.IntervalIdentifier,
					IsRemoved = entry.IsRemoved,
					ProfileType = entry.ProfileType,
					Operation = LeaderboardUpdateOperation.Delete,
					ExistingEntryId = entry.LeaderboardEntryId
				});
			}
		}

		foreach (var (slug, definition) in registrationService.LeaderboardsById) {
			if (definition is not IProfileLeaderboardDefinition profileLb) continue;

			var useIncrease = definition.Info.UseIncreaseForInterval;
			if (!leaderboardsIdsBySlug.TryGetValue(slug, out var lb)) continue;

			var type = GetTypeFromSlug(slug);
			var intervalIdentifier = type switch {
				LeaderboardType.Monthly => monthlyInterval,
				LeaderboardType.Weekly => weeklyInterval,
				_ => null
			};

			var score = profileLb.GetScoreFromProfile(profile, type);
			if (score == -1 && profile.Garden is not null) {
				if (intervalIdentifier is not null) {
					var valid = IsWithinInterval(type, profile.Garden.LastUpdated);
					if (!valid) continue;
				}

				score = profileLb.GetScoreFromGarden(profile.Garden, type);
			}

			if (existingEntries.TryGetValue(lb.LeaderboardId, out var entry)) {
				var changed = false;
				var newProfileType = entry.ProfileType;
				var newIsRemoved = entry.IsRemoved;
				var newScore = entry.Score;

				if (entry.IsRemoved != profile.IsDeleted) {
					newIsRemoved = profile.IsDeleted;
					changed = true;
				}

				if (profile.GameMode != entry.ProfileType) {
					newProfileType = profile.GameMode;
					changed = true;
				}

				if (score >= 0 && (score >= entry.InitialScore || !useIncrease)) {
					var calculatedScore = entry.IntervalIdentifier is not null && useIncrease
						? score - entry.InitialScore
						: score;

					if (entry.Score != calculatedScore) {
						newScore = calculatedScore;
						changed = true;
					}
				}

				if (changed) {
					updates.Add(new LeaderboardUpdateEntry {
						LeaderboardId = lb.LeaderboardId,
						ProfileId = profile.ProfileId,
						Score = newScore,
						InitialScore = entry.InitialScore,
						IntervalIdentifier = entry.IntervalIdentifier,
						IsRemoved = newIsRemoved,
						ProfileType = newProfileType,
						Operation = LeaderboardUpdateOperation.Update,
						ExistingEntryId = entry.LeaderboardEntryId
					});
				}

				continue;
			}

			if (score <= 0 || score < lb.MinimumScore) continue;

			updates.Add(new LeaderboardUpdateEntry {
				LeaderboardId = lb.LeaderboardId,
				ProfileId = profile.ProfileId,
				Score = useIncrease && intervalIdentifier is not null ? 0 : score,
				InitialScore = useIncrease && intervalIdentifier is not null ? score : 0,
				IntervalIdentifier = intervalIdentifier,
				IsRemoved = profile.IsDeleted,
				ProfileType = profile.GameMode,
				Operation = LeaderboardUpdateOperation.Insert
			});
		}

		return updates;
	}

	private async Task RestoreProfileLeaderboards(string profileId, CancellationToken c = default) {
		await context.LeaderboardEntries
			.Where(e => e.ProfileId == profileId && e.IsRemoved == true)
			.ExecuteUpdateAsync(s => s.SetProperty(le => le.IsRemoved, false), c);
	}

	public async Task<PlayerLeaderboardEntryWithRankDto?> GetLeaderboardEntryAsync(string leaderboardSlug,
		string memberOrProfileId, string? gameMode = null, RemovedFilter removedFilter = RemovedFilter.NotRemoved,
		string? identifier = null) {
		if (!registrationService.LeaderboardsById.TryGetValue(leaderboardSlug, out var definition)) return null;

		var lb = await context.Leaderboards
			.AsNoTracking()
			.FirstOrDefaultAsync(lb => lb.Slug == leaderboardSlug);
		if (lb is null) return null;

		identifier ??= GetCurrentIdentifier(GetTypeFromSlug(leaderboardSlug));

		var entry = await (definition.IsMemberLeaderboard()
				? context.LeaderboardEntries
					.Where(e => e.ProfileMemberId == Guid.Parse(memberOrProfileId) &&
					            e.LeaderboardId == lb.LeaderboardId)
					.EntryFilter(identifier, removedFilter, gameMode)
				: context.LeaderboardEntries
					.Where(e => e.ProfileId == memberOrProfileId && e.LeaderboardId == lb.LeaderboardId)
					.EntryFilter(identifier, removedFilter, gameMode))
			.Select(le => new {
				le.LeaderboardEntryId,
				le.LeaderboardId,
				le.IntervalIdentifier,
				le.Score,
				le.InitialScore,
				le.IsRemoved,
				le.ProfileType,
				le.Leaderboard // Include the Leaderboard for its Title/Slug etc.
			})
			.FirstOrDefaultAsync();
		if (entry is null) return null;

		// Calculate the rank for this specific entry
		var rank = entry.IsRemoved
			? -1
			: await GetRankForEntryAsync(
				entry.LeaderboardId,
				identifier,
				entry.Score,
				entry.LeaderboardEntryId
			);

		return new PlayerLeaderboardEntryWithRankDto {
			IntervalIdentifier = entry.IntervalIdentifier,
			Amount = (double)entry.Score,
			InitialAmount = (double)entry.InitialScore,
			Profile = definition.IsProfileLeaderboard(),
			Rank = rank,
			Title = entry.Leaderboard.Title,
			Slug = entry.Leaderboard.Slug,
			Short = entry.Leaderboard.ShortTitle,
			Type = entry.Leaderboard.ScoreDataType
		};
	}

	public async Task<int> GetRankForEntryAsync(
		int leaderboardId,
		string? intervalIdentifier,
		decimal score,
		int leaderboardEntryId) {
		var sql = """
		          SELECT COUNT(*) + 1
		          FROM "LeaderboardEntries" AS o
		          WHERE o."IsRemoved" = false
		            AND o."LeaderboardId" = @leaderboardId
		            AND (o."Score", o."LeaderboardEntryId") > (@score, @leaderboardEntryId)
		          """;

		if (intervalIdentifier != null) {
			sql += """ AND o."IntervalIdentifier" = @intervalIdentifier""";
		}
		else {
			sql += """ AND o."IntervalIdentifier" IS NULL""";
		}

		var parameters = new[] {
			new Npgsql.NpgsqlParameter("leaderboardId", leaderboardId),
			new Npgsql.NpgsqlParameter("score", score),
			new Npgsql.NpgsqlParameter("leaderboardEntryId", leaderboardEntryId),
			new Npgsql.NpgsqlParameter("intervalIdentifier", (object?)intervalIdentifier ?? DBNull.Value)
		};

		var connection = context.Database.GetDbConnection();
		await connection.OpenAsync();
		await using var command = connection.CreateCommand();
		command.CommandText = sql;
		command.Parameters.AddRange(parameters);

		var rank = (long?)await command.ExecuteScalarAsync() ?? -1;
		await connection.CloseAsync();

		return (int)rank;
	}

	public async Task<Dictionary<string, LeaderboardPositionDto?>> GetMultipleLeaderboardRanks(
		List<string> leaderboards, string playerUuid, string profileId, int? upcoming = null, int? previous = null,
		int? atRank = null, string? gameMode = null, RemovedFilter removedFilter = RemovedFilter.NotRemoved,
		string? identifier = null, CancellationToken? c = null) {
		var memberId = profileId;

		var member = await context.ProfileMembers
			.Where(p => p.ProfileId.Equals(profileId) && p.PlayerUuid.Equals(playerUuid))
			.Select(p => new { p.Id, p.LastUpdated, p.PlayerUuid })
			.FirstOrDefaultAsync(c ?? CancellationToken.None);

		if (member is not null && member.Id != Guid.Empty) {
			await memberService.UpdatePlayerIfNeeded(member.PlayerUuid, 5);

			memberId = member.Id.ToString();
		}

		var result = new Dictionary<string, LeaderboardPositionDto?>();

		foreach (var leaderboard in leaderboards) {
			var resourceId = memberId;
			if (registrationService.LeaderboardsById.TryGetValue(leaderboard, out var definition) &&
			    definition.IsProfileLeaderboard())
				resourceId = profileId; // If the leaderboard is a profile leaderboard, use the profile ID

			result[leaderboard] = await GetLeaderboardRankByResourceId(
				leaderboard,
				resourceId,
				upcoming,
				previous,
				atRank,
				atAmount: null,
				gameMode,
				removedFilter,
				identifier,
				c: c
			);
		}

		return result;
	}

	public async Task<LeaderboardPositionDto> GetLeaderboardRank(
		string leaderboardId, string playerUuid, string profileId, int? upcoming = null, int? previous = null,
		int? atRank = null, double? atAmount = null, string? gameMode = null,
		RemovedFilter removedFilter = RemovedFilter.NotRemoved,
		string? identifier = null, bool skipUpdate = false, CancellationToken? c = null) {
		var memberId = profileId;

		if (registrationService.LeaderboardsById.TryGetValue(leaderboardId, out var definition) &&
		    definition.IsMemberLeaderboard()) {
			// Set the memberId to the profile member ID if the leaderboard is not a profile leaderboard
			var member = await context.ProfileMembers
				.Where(p => p.ProfileId.Equals(profileId) && p.PlayerUuid.Equals(playerUuid))
				.Select(p => new { p.Id, p.LastUpdated, p.PlayerUuid, SkillsApiEnabled = p.Api.Skills })
				.FirstOrDefaultAsync(c ?? CancellationToken.None);

			if (member is null || member.Id == Guid.Empty) {
				memberId = null;
			}
			else {
				if (!skipUpdate && member.SkillsApiEnabled) {
					await memberService.UpdatePlayerIfNeeded(member.PlayerUuid, RequestedResources.ProfilesOnly with {
						CooldownMultiplier = 10,
						RequireActiveMemberId = member.Id
					});
				}

				memberId = member.Id.ToString();
			}
		}

		var result = memberId is not null
			? await GetLeaderboardRankByResourceId(
				leaderboardId,
				memberId,
				upcoming,
				previous,
				atRank,
				atAmount,
				gameMode,
				removedFilter,
				identifier,
				skipUpdate,
				c)
			: null;

		if (result is not null) return result;

		var last = await GetLastLeaderboardEntry(
			leaderboardId,
			removedFilter: removedFilter,
			gameMode: gameMode,
			identifier: identifier);

		return new LeaderboardPositionDto {
			Rank = -1,
			Amount = 0,
			MinAmount = GetLeaderboardMinScore(leaderboardId),
			UpcomingRank = last?.Rank ?? 10_000,
			UpcomingPlayers = upcoming > 0 && last is not null ? [last] : null
		};
	}

	public async Task<LeaderboardPositionDto?> GetLeaderboardRankByResourceId(
		string leaderboardId, string resourceId, int? upcoming = null, int? previous = null,
		int? atRank = null, double? atAmount = null, string? gameMode = null,
		RemovedFilter removedFilter = RemovedFilter.NotRemoved,
		string? identifier = null, bool skipUpdate = false, CancellationToken? c = null) {
		if (!registrationService.LeaderboardsById.TryGetValue(leaderboardId, out var definition)) return null;

		identifier ??= GetCurrentIdentifier(GetTypeFromSlug(leaderboardId));
		var cancellationToken = c ?? CancellationToken.None;

		var leaderboard = await context.Leaderboards
			.AsNoTracking()
			.FirstOrDefaultAsync(lb => lb.Slug == leaderboardId, cancellationToken);
		if (leaderboard is null) return null;

		Guid? memberId = null;
		if (definition.IsMemberLeaderboard()) {
			if (!Guid.TryParse(resourceId, out var parsedMemberId)) return null;
			memberId = parsedMemberId;
		}

		var baseQuery = context.LeaderboardEntries
			.AsNoTracking()
			.FromLeaderboard(leaderboard.LeaderboardId, definition.IsMemberLeaderboard())
			.EntryFilter(identifier, removedFilter, gameMode);

		var userEntry = await (definition.IsMemberLeaderboard()
				? baseQuery.Where(e => e.ProfileMemberId == memberId)
				: baseQuery.Where(e => e.ProfileId == resourceId))
			.Select(e => new {
				e.LeaderboardEntryId,
				e.Score,
				e.InitialScore,
				e.IsRemoved,
				e.ProfileType
			})
			.FirstOrDefaultAsync(cancellationToken);

		if (userEntry is null) return null;

		var userScore = userEntry.Score;
		var userId = userEntry.LeaderboardEntryId;

		var rankCount = userEntry.IsRemoved
			? -1
			: await baseQuery.CountAsync(
				e => e.Score > userScore || (e.Score == userScore && e.LeaderboardEntryId > userId),
				cancellationToken);

		// Anchor window start
		var anchorScore = userScore;
		var anchorId = userId;
		var anchorRank = rankCount == -1 ? -1 : rankCount + 1;
		var usingAtAmount = atAmount is > 0 && (decimal)atAmount > userScore;

		if (atRank is > 0 && (anchorRank == -1 || atRank < anchorRank)) {
			var bucketedRank = GetBucketedRank(atRank.Value);
			var anchorCacheKey =
				$"lb:anchor:{leaderboardId}:{gameMode ?? "all"}:{identifier ?? "c"}:{removedFilter}:{bucketedRank}";
			var cacheOptions = GetCacheOptions(bucketedRank);

			var anchorCacheMiss = false;
			var cachedAnchor = await cache.GetOrCreateAsync(anchorCacheKey, async ct => {
				anchorCacheMiss = true;
				var anchor = await baseQuery
					.OrderByDescending(e => e.Score)
					.ThenByDescending(e => e.LeaderboardEntryId)
					.Skip(bucketedRank - 1)
					.Select(e => new CachedAnchor(e.Score, e.LeaderboardEntryId))
					.FirstOrDefaultAsync(ct);
				return anchor;
			}, cacheOptions, cancellationToken: cancellationToken);

			if (anchorCacheMiss) {
				cacheMetrics.RecordAnchorCacheMiss(leaderboardId, bucketedRank);
			}
			else {
				cacheMetrics.RecordAnchorCacheHit(leaderboardId, bucketedRank);
			}

			if (cachedAnchor is not null) {
				anchorScore = cachedAnchor.Score;
				anchorId = cachedAnchor.EntryId;
				anchorRank = bucketedRank;
			}
		}
		else if (usingAtAmount && atAmount is not null) {
			anchorScore = (decimal)atAmount.Value;
			anchorId = 0; // Will be filtered out by the > comparison in upcoming query
			anchorRank = await baseQuery.CountAsync(
				e => e.Score > (decimal)atAmount.Value, cancellationToken: cancellationToken) + 1;
		}

		var upcomingPlayers = new List<LeaderboardEntryWithRankDto>();
		var previousPlayers = new List<LeaderboardEntryDto>();

		if (upcoming > 0 && !userEntry.IsRemoved) {
			var bucketedUpcoming = GetBucketedUpcoming(upcoming.Value);
			var upcomingCacheKey =
				$"lb:upcoming:{leaderboardId}:{gameMode ?? "all"}:{identifier ?? "c"}:{removedFilter}:{definition.IsMemberLeaderboard()}:{anchorRank}:{bucketedUpcoming}";
			var cacheOptions = GetCacheOptions(anchorRank > 0 ? anchorRank : 50000);

			var upcomingCacheMiss = false;
			var cachedUpcoming = await cache.GetOrCreateAsync(upcomingCacheKey, async ct => {
				upcomingCacheMiss = true;
				var upcomingQuery = baseQuery
					.Where(e => e.Score > anchorScore || (e.Score == anchorScore && e.LeaderboardEntryId > anchorId))
					.OrderBy(e => e.Score)
					.ThenBy(e => e.LeaderboardEntryId)
					.Take(bucketedUpcoming);

				var list = definition.IsMemberLeaderboard()
					? await upcomingQuery.MapToMemberLeaderboardEntries(includeMeta: false).ToListAsync(ct)
					: await upcomingQuery.MapToProfileLeaderboardEntries(removedFilter).ToListAsync(ct);

				return list.Select((entry, i) => entry.ToDtoWithRank(anchorRank - i - 1)).ToList();
			}, cacheOptions, cancellationToken: cancellationToken);

			if (upcomingCacheMiss) {
				cacheMetrics.RecordUpcomingCacheMiss(leaderboardId, anchorRank > 0 ? anchorRank : 50000);
			}
			else {
				cacheMetrics.RecordUpcomingCacheHit(leaderboardId, anchorRank > 0 ? anchorRank : 50000);
			}

			if (usingAtAmount) {
				upcomingPlayers = cachedUpcoming.Where(e => atAmount < e.Amount && e.Uuid != resourceId)
					.Take(upcoming.Value).ToList();
			}
			else {
				upcomingPlayers = cachedUpcoming.Where(e => e.Uuid != resourceId).Take(upcoming.Value).ToList();
			}

			var next = upcomingPlayers.FirstOrDefault();
			if (next is not null && next.Rank > 0) {
				anchorRank = next.Rank + 1;
			}
		}

		if (previous > 0 && !userEntry.IsRemoved) {
			var previousQuery = baseQuery
				.Where(e => e.Score < anchorScore || (e.Score == anchorScore && e.LeaderboardEntryId < anchorId))
				.OrderByDescending(e => e.Score)
				.ThenByDescending(e => e.LeaderboardEntryId)
				.Take(previous.Value);

			previousPlayers = definition.IsMemberLeaderboard()
				? await previousQuery.MapToMemberLeaderboardEntries(includeMeta: false).ToListAsync(cancellationToken)
				: await previousQuery.MapToProfileLeaderboardEntries(removedFilter).ToListAsync(cancellationToken);
		}

		var position = rankCount == -1 ? -1 : rankCount + 1;
		var windowRank = anchorRank > 0 ? anchorRank : position;

		var result = new LeaderboardPositionDto {
			Rank = position,
			Amount = (double)userEntry.Score,
			InitialAmount = (double)userEntry.InitialScore,
			MinAmount = (double)definition.Info.MinimumScore,
			UpcomingRank = windowRank == -1 ? -1 : windowRank - 1,
			UpcomingPlayers = upcomingPlayers,
			Previous = previousPlayers
		};

		return result;
	}

	public static string? GetCurrentIdentifier(LeaderboardType type) {
		switch (type) {
			case LeaderboardType.Current:
				return null;
			case LeaderboardType.Weekly:
				var now = DateTime.UtcNow;
				var isoYear = ISOWeek.GetYear(now);
				var week = ISOWeek.GetWeekOfYear(now);
				return $"{isoYear}-W{week.ToString().PadLeft(2, '0')}";
			case LeaderboardType.Monthly:
				return DateTime.UtcNow.ToString("yyyy-MM", CultureInfo.InvariantCulture);
			default:
				throw new ArgumentOutOfRangeException(nameof(type), type, null);
		}
	}

	public (long start, long end) GetCurrentTimeRange(LeaderboardType type) {
		return GetIntervalTimeRange(type, DateTimeOffset.UtcNow);
	}

	public (long start, long end) GetIntervalTimeRange(LeaderboardType type, DateTimeOffset now) {
		switch (type) {
			case LeaderboardType.Current:
				return (0, 0);
			case LeaderboardType.Weekly:
				var nowUtc = now.UtcDateTime;
				var isoYear = ISOWeek.GetYear(nowUtc);
				var isoWeekNumber = ISOWeek.GetWeekOfYear(nowUtc);

				var startOfWeekUtc = ISOWeek.ToDateTime(isoYear, isoWeekNumber, DayOfWeek.Monday).ToUniversalTime();
				var endOfWeekUtc = isoWeekNumber == ISOWeek.GetWeeksInYear(isoYear)
					? ISOWeek.ToDateTime(isoYear + 1, 1, DayOfWeek.Monday).ToUniversalTime()
					: ISOWeek.ToDateTime(isoYear, isoWeekNumber + 1, DayOfWeek.Monday).ToUniversalTime();

				var startTimestamp = ((DateTimeOffset)startOfWeekUtc).ToUnixTimeSeconds();
				var endTimestamp = ((DateTimeOffset)endOfWeekUtc).ToUnixTimeSeconds();

				return (startTimestamp, endTimestamp);
			case LeaderboardType.Monthly:
				var startOfMonth = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, now.Offset);
				var endOfMonth = startOfMonth.AddMonths(1);

				return (startOfMonth.ToUnixTimeSeconds(), endOfMonth.ToUnixTimeSeconds());
			default:
				throw new ArgumentOutOfRangeException(nameof(type), type, null);
		}
	}

	public (long start, long end) GetIntervalTimeRange(string? interval) {
		if (interval.IsNullOrEmpty() || !interval.Contains('-')) {
			return (0, 0);
		}

		if (interval.Contains("-W")) {
			var split = interval.Split("-W");
			if (int.TryParse(split[0], out var year) && int.TryParse(split[1], out var week)) {
				return GetIntervalTimeRange(LeaderboardType.Weekly, ISOWeek.ToDateTime(year, week, DayOfWeek.Monday));
			}

			return (0, 0);
		}

		var monthSplit = interval.Split("-");
		if (int.TryParse(monthSplit[0], out var monthlyYear) && int.TryParse(monthSplit[1], out var month)) {
			return GetIntervalTimeRange(LeaderboardType.Monthly,
				new DateTimeOffset(monthlyYear, month, 1, 0, 0, 0, DateTimeOffset.UtcNow.Offset));
		}

		return (0, 0);
	}

	public bool IsWithinInterval(LeaderboardType type, DateTimeOffset point) {
		var (start, end) = GetCurrentTimeRange(type);
		if (start == 0 && end == 0) return true;

		var current = point.ToUnixTimeSeconds();
		return start <= current && end >= current;
	}

	public static LeaderboardType GetTypeFromSlug(string slug) {
		return slug switch {
			_ when slug.EndsWith("-monthly") => LeaderboardType.Monthly,
			_ when slug.EndsWith("-weekly") => LeaderboardType.Weekly,
			_ => LeaderboardType.Current
		};
	}

	public async Task<List<PlayerLeaderboardEntryWithRankDto>> GetPlayerLeaderboardEntriesWithRankAsync(
		Guid profileMemberId) {
		var monthlyInterval = GetCurrentIdentifier(LeaderboardType.Monthly);
		var weeklyInterval = GetCurrentIdentifier(LeaderboardType.Weekly);

		var sql = @"
	        SELECT le.""IntervalIdentifier"", le.""Score"", le.""InitialScore"", l.""Title"", l.""Slug"", l.""ShortTitle"", l.""ScoreDataType"",
	               CASE WHEN le.""IsRemoved"" = true THEN -1 ELSE (SELECT COUNT(*) + 1 FROM ""LeaderboardEntries"" o WHERE o.""IsRemoved"" = false AND o.""LeaderboardId"" = le.""LeaderboardId"" AND o.""IntervalIdentifier"" = le.""IntervalIdentifier"" AND (o.""Score"", o.""LeaderboardEntryId"") > (le.""Score"", le.""LeaderboardEntryId"")) END AS ""Rank""
	        FROM ""LeaderboardEntries"" AS le
	        INNER JOIN ""Leaderboards"" AS l ON le.""LeaderboardId"" = l.""LeaderboardId""
	        WHERE le.""ProfileMemberId"" = @profileMemberId AND le.""IntervalIdentifier"" IN (@monthlyInterval, @weeklyInterval) AND le.""Score"" > 0

	        UNION ALL

	        SELECT le.""IntervalIdentifier"", le.""Score"", le.""InitialScore"", l.""Title"", l.""Slug"", l.""ShortTitle"", l.""ScoreDataType"",
	               CASE WHEN le.""IsRemoved"" = true THEN -1 ELSE (SELECT COUNT(*) + 1 FROM ""LeaderboardEntries"" o WHERE o.""IsRemoved"" = false AND o.""LeaderboardId"" = le.""LeaderboardId"" AND o.""IntervalIdentifier"" IS NULL AND (o.""Score"", o.""LeaderboardEntryId"") > (le.""Score"", le.""LeaderboardEntryId"")) END AS ""Rank""
	        FROM ""LeaderboardEntries"" AS le
	        INNER JOIN ""Leaderboards"" AS l ON le.""LeaderboardId"" = l.""LeaderboardId""
	        WHERE le.""ProfileMemberId"" = @profileMemberId AND le.""IntervalIdentifier"" IS NULL AND le.""Score"" > 0;
	    ";

		var results = await context.Set<LeaderboardRanksQueryResult>()
			.FromSqlRaw(sql,
				new Npgsql.NpgsqlParameter("profileMemberId", profileMemberId),
				new Npgsql.NpgsqlParameter("monthlyInterval", monthlyInterval),
				new Npgsql.NpgsqlParameter("weeklyInterval", weeklyInterval))
			.ToListAsync();

		return results.Select(r => new PlayerLeaderboardEntryWithRankDto {
			IntervalIdentifier = r.IntervalIdentifier,
			Amount = (double)r.Score,
			InitialAmount = (double)r.InitialScore,
			Rank = (int)r.Rank,
			Title = r.Title,
			Slug = r.Slug,
			Short = r.ShortTitle,
			Type = Enum.Parse<LeaderboardScoreDataType>(r.ScoreDataType)
		}).ToList();
	}

	public async Task<List<PlayerLeaderboardEntryWithRankDto>> GetProfileLeaderboardEntriesWithRankAsync(
		string profileId) {
		var monthlyInterval = GetCurrentIdentifier(LeaderboardType.Monthly);
		var weeklyInterval = GetCurrentIdentifier(LeaderboardType.Weekly);

		var sql = @"
	        SELECT le.""IntervalIdentifier"", le.""Score"", le.""InitialScore"", l.""Title"", l.""Slug"", l.""ShortTitle"", l.""ScoreDataType"",
	               CASE WHEN le.""IsRemoved"" = true THEN -1 ELSE (SELECT COUNT(*) + 1 FROM ""LeaderboardEntries"" o WHERE o.""IsRemoved"" = false AND o.""LeaderboardId"" = le.""LeaderboardId"" AND o.""IntervalIdentifier"" = le.""IntervalIdentifier"" AND (o.""Score"", o.""LeaderboardEntryId"") > (le.""Score"", le.""LeaderboardEntryId"")) END AS ""Rank""
	        FROM ""LeaderboardEntries"" AS le
	        INNER JOIN ""Leaderboards"" AS l ON le.""LeaderboardId"" = l.""LeaderboardId""
	        WHERE le.""ProfileId"" = @profileId AND le.""IntervalIdentifier"" IN (@monthlyInterval, @weeklyInterval)

	        UNION ALL

	        SELECT le.""IntervalIdentifier"", le.""Score"", le.""InitialScore"", l.""Title"", l.""Slug"", l.""ShortTitle"", l.""ScoreDataType"",
	               CASE WHEN le.""IsRemoved"" = true THEN -1 ELSE (SELECT COUNT(*) + 1 FROM ""LeaderboardEntries"" o WHERE o.""IsRemoved"" = false AND o.""LeaderboardId"" = le.""LeaderboardId"" AND o.""IntervalIdentifier"" IS NULL AND (o.""Score"", o.""LeaderboardEntryId"") > (le.""Score"", le.""LeaderboardEntryId"")) END AS ""Rank""
	        FROM ""LeaderboardEntries"" AS le
	        INNER JOIN ""Leaderboards"" AS l ON le.""LeaderboardId"" = l.""LeaderboardId""
	        WHERE le.""ProfileId"" = @profileId AND le.""IntervalIdentifier"" IS NULL;
	    ";

		var results = await context.Set<LeaderboardRanksQueryResult>()
			.FromSqlRaw(sql,
				new Npgsql.NpgsqlParameter("profileId", profileId),
				new Npgsql.NpgsqlParameter("monthlyInterval", monthlyInterval),
				new Npgsql.NpgsqlParameter("weeklyInterval", weeklyInterval))
			.ToListAsync();

		return results.Select(r => new PlayerLeaderboardEntryWithRankDto {
			IntervalIdentifier = r.IntervalIdentifier,
			Amount = (double)r.Score,
			InitialAmount = (double)r.InitialScore,
			Profile = true,
			Rank = (int)r.Rank,
			Title = r.Title,
			Slug = r.Slug,
			Short = r.ShortTitle,
			Type = Enum.Parse<LeaderboardScoreDataType>(r.ScoreDataType)
		}).ToList();
	}

	public async Task<List<LeaderboardEntryDto>> GetGuildMembersLeaderboardEntriesAsync(string guildId,
		string leaderboardSlug,
		string? identifier = null, string? gameMode = null) {
		// Resolve leaderboard definition and leaderboard
		if (!registrationService.LeaderboardsById.TryGetValue(leaderboardSlug, out var definition)) return [];

		var lb = await context.Leaderboards.AsNoTracking().FirstOrDefaultAsync(l => l.Slug == leaderboardSlug);
		if (lb is null) return [];

		identifier ??= GetCurrentIdentifier(GetTypeFromSlug(leaderboardSlug));

		// Get active guild members' player uuids
		var guildMemberUuids = await context.HypixelGuildMembers
			.AsNoTracking()
			.Where(gm => gm.GuildId == guildId && gm.Active)
			.Select(gm => gm.PlayerUuid)
			.ToListAsync();

		if (guildMemberUuids.Count == 0) return [];

		// Get corresponding profile members for those player uuids
		var profileMembers = await context.ProfileMembers
			.AsNoTracking()
			.Where(pm => guildMemberUuids.Contains(pm.PlayerUuid) && !pm.WasRemoved)
			.Select(pm => new { pm.Id, pm.ProfileId })
			.ToListAsync();

		var memberIds = profileMembers.Select(pm => pm.Id).ToList();
		var profileIds = profileMembers.Select(pm => pm.ProfileId).Distinct().ToList();

		if (memberIds.Count == 0) return [];

		// Make a nullable list to match the ProfileMemberId nullable type on LeaderboardEntry
		var memberIdsNullable = memberIds.Select(g => (Guid?)g).ToList();

		// Fetch leaderboard entries for these members or profiles depending on leaderboard type
		if (definition.IsMemberLeaderboard()) {
			return await context.LeaderboardEntries.AsNoTracking()
				.FromLeaderboard(lb.LeaderboardId, true)
				.EntryFilter(identifier, RemovedFilter.NotRemoved, gameMode)
				.Include(p => p.ProfileMember)
				.Where(e => memberIdsNullable.Contains(e.ProfileMemberId))
				.OrderByDescending(e => e.Score)
				.ThenByDescending(e => e.LeaderboardEntryId)
				.MapToMemberLeaderboardEntries(true)
				.ToListAsync();
		}

		return await context.LeaderboardEntries.AsNoTracking()
			.FromLeaderboard(lb.LeaderboardId, false)
			.EntryFilter(identifier, RemovedFilter.NotRemoved, gameMode)
			.Include(p => p.Profile)
			.Where(e => profileIds.Contains(e.ProfileId!))
			.OrderByDescending(e => e.Score)
			.ThenByDescending(e => e.LeaderboardEntryId)
			.MapToProfileLeaderboardEntries()
			.ToListAsync();
	}
}

public class PlayerLeaderboardEntryWithRankDto
{
	public required string Title { get; set; }

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Short { get; set; }

	public required string Slug { get; set; }

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public bool? Profile { get; set; }

	public int Rank { get; set; }

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? IntervalIdentifier { get; set; }

	public double Amount { get; set; }

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public double InitialAmount { get; set; }

	[JsonConverter(typeof(JsonStringEnumConverter<LeaderboardScoreDataType>))]
	public LeaderboardScoreDataType Type { get; set; }
}

public enum RemovedFilter
{
	NotRemoved = 0,
	Removed = 1,
	All = 2
}

public class ProfileLeaderboardMember
{
	public required string Ign { get; init; }
	public required string Uuid { get; init; }
	public int Xp { get; init; }
}

public class LeaderboardEntry
{
	public required string MemberId { get; init; }
	public string? Ign { get; init; }
	public string? Profile { get; init; }
	public double Amount { get; init; }
	public string? Uuid { get; init; }
	public List<ProfileLeaderboardMember>? Members { get; init; }
}

public class LeaderboardEntryWithRank : LeaderboardEntry
{
	public int Rank { get; init; }
}