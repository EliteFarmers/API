using EliteAPI.Configuration.Settings;
using EliteAPI.Data;
using EliteAPI.Features.Account.Models;
using EliteAPI.Features.HypixelGuilds.Models;
using EliteAPI.Parsers.Profiles;
using EliteAPI.Services.Interfaces;
using EliteAPI.Utilities;
using EliteFarmers.HypixelAPI;
using EliteFarmers.HypixelAPI.DTOs;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;

namespace EliteAPI.Features.HypixelGuilds.Services;

public interface IHypixelGuildService
{
	Task UpdateGuildIfNeeded(MinecraftAccount account, CancellationToken c = default);
	Task UpdateGuildIfNeeded(string guildId, CancellationToken c = default);
	Task<List<HypixelGuildDetailsDto>> GetGuildListAsync(HypixelGuildListQuery query, CancellationToken c = default);
	Task<IReadOnlyList<HypixelGuildSearchResultDto>> SearchGuildsAsync(string query, int limit,
        CancellationToken c = default);
	Task<(int rank, double amount)> GetGuildRankAsync(string guildId, SortHypixelGuildsBy? sortBy, 
		string? collection, string? skill, CancellationToken c = default);
	Task<int> GetGuildLeaderboardTotalCount(HypixelGuildListQuery query, CancellationToken c = default);
}

[RegisterService<IHypixelGuildService>(LifeTime.Scoped)]
public class HypixelGuildService(
	DataContext context,
	IHttpContextAccessor httpContextAccessor,
	HybridCache hybridCache,
	IMojangService mojangService,
	ILogger<HypixelGuildService> logger,
	IHypixelApi hypixelApi,
	HybridCache cache,
	IOptions<ConfigCooldownSettings> cooldownSettings
	) : IHypixelGuildService
{
	private readonly ConfigCooldownSettings _coolDowns = cooldownSettings.Value;
	
	public async Task UpdateGuildIfNeeded(MinecraftAccount account, CancellationToken c = default) {
		if (httpContextAccessor.HttpContext is not null && httpContextAccessor.HttpContext.IsKnownBot()) {
			return;
		}
		
		var member = await context.HypixelGuildMembers
			.FirstOrDefaultAsync(m => m.PlayerUuid == account.Id && m.Active, cancellationToken: c);

		try {
			if (member is not null) {
				await UpdateGuild(member.GuildId, c);
				return;
			}

			if (account.GuildLastUpdated.OlderThanSeconds(_coolDowns.HypixelGuildMemberCooldown)) {
				account.GuildLastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
				
				var hypixelXp = await context.PlayerData
					.Where(p => p.Uuid == account.Id)
					.Select(p => p.NetworkExp)
					.FirstOrDefaultAsync(cancellationToken: c);

				// Limit automatic guild updates to players with at least Hypixel level 100
				if (SkillParser.GetNetworkLevel(hypixelXp) >= 100) {
					await UpdateGuildByPlayerUuid(account.Id, c);
				}
			}
		} catch (Exception e) {
			logger.LogError(e, "Failed to update guild");
		}
	}

	public async Task UpdateGuildIfNeeded(string guildId, CancellationToken c = default) {
		await UpdateGuild(guildId, c);
	}

	private async Task UpdateGuild(string guildId, CancellationToken c = default) {
		await hybridCache.GetOrCreateAsync(guildId, async (ct) => {
			var guildLastUpdated = await context.HypixelGuilds
				.Where(g => g.Id == guildId)
				.Select(g => g.LastUpdated)
				.FirstOrDefaultAsync(cancellationToken: ct);

			if (!guildLastUpdated.OlderThanSeconds(_coolDowns.HypixelGuildCooldown)) {
				return true;
			}
			
			var guildResponse = await hypixelApi.FetchGuildByIdAsync(guildId, ct);
			var guild = guildResponse.Content?.Guild;
		
			if (!guildResponse.IsSuccessful) {
				logger.LogWarning("Failed to fetch guild {GuildId}", guildId);
				return true;
			}
			
			if (guildResponse.Content.Success && guild is null) {
				// Guild deleted
				var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
				
				// Uncomment after confirming no issues with this detection
				// await context.HypixelGuildMembers
				// 	.Where(m => m.GuildId == guildId && m.Active)
				// 	.ExecuteUpdateAsync(s => s
				// 			.SetProperty(e => e.Active, false)
				// 			.SetProperty(e => e.LeftAt, now),
				// 		cancellationToken: ct);
				
				var existingGuild = await context.HypixelGuilds
					.FirstOrDefaultAsync(g => g.Id == guildId, ct);
				
				if (existingGuild is not null) {
					// existingGuild.MemberCount = 0;
					existingGuild.LastUpdated = now;
					existingGuild.Deleted = true;
					await context.SaveChangesAsync(ct);
				}
				
				return true;
			}
			
			if (guild is null) {
				logger.LogWarning("Failed to fetch guild {GuildId}", guildId);
				return true;
			}
			
			await UpdateGuildInternal(guild, guildId, ct);
			
			// Queue stats update
			await new HypixelGuildStatUpdateCommand { GuildId = guildId }.QueueJobAsync(ct: ct);
			
			return true;
		}, new HybridCacheEntryOptions() {
			LocalCacheExpiration = TimeSpan.FromMinutes(1),
			Expiration = TimeSpan.FromMinutes(1)
		}, cancellationToken: c);
	}
	
	private async Task UpdateGuildInternal(RawHypixelGuild guild, string guildId, CancellationToken c = default) {
		var sevenDaysAgo = DateTimeOffset.UtcNow.AddDays(-7);
		var date = new DateOnly(sevenDaysAgo.Year, sevenDaysAgo.Month, sevenDaysAgo.Day);

		var playerUuids = guild.Members.Select(m => m.Uuid).ToList();
		
		var existing = await context.HypixelGuilds
			.IgnoreQueryFilters()
			.Include(g => g.Members.Where(m => m.Active || playerUuids.Contains(m.PlayerUuid)))
			.ThenInclude(m => m.ExpHistory.Where(e => e.Day >= date))
			.FirstOrDefaultAsync(g => g.Id == guild.Id, c);

		if (existing is null) {
			existing = new HypixelGuild() {
				Id = guild.Id,
				Name = guild.Name,
				NameLower = guild.Name.ToLowerInvariant(),
				CreatedAt = guild.Created,
			};
			
			context.HypixelGuilds.Add(existing);
		}

		existing.MemberCount = guild.Members.Count;
		existing.Name = guild.Name;
		existing.NameLower = guild.Name.ToLowerInvariant();
		existing.CreatedAt = guild.Created;
		existing.Description = guild.Description ?? string.Empty;
		existing.PreferredGames = guild.PreferredGames;
		existing.PubliclyListed = guild.PublicallyListed;
		existing.Exp = guild.Exp;
		existing.Tag = guild.Tag;
		existing.TagColor = guild.TagColor;
		existing.GameExp = guild.GuildExpByGameType;
		existing.Ranks = guild.Ranks;
		existing.LastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
		existing.Deleted = false;
		
		// Changes are saved in UpdateGuildMembers
		await UpdateGuildMembers(existing, guild.Members, c);
	}

	private async Task UpdateGuildMembers(HypixelGuild guild, List<RawHypixelGuildMember> newMembers, CancellationToken c = default) {
		var current = guild.Members
			.GroupBy(p => p.PlayerUuid)
			.ToDictionary(m => m.Key, m => m.First());
		
		var batchGet = await mojangService.GetMinecraftAccounts(newMembers.Select(m => m.Uuid).ToList());

		foreach (var member in newMembers) {
			if (!batchGet.TryGetValue(member.Uuid, out var accountMeta) || accountMeta is null) continue;
			
			var xpHistory = member.ExpHistory.ToDictionary(kvp => {
				var split = kvp.Key.Split('-');
				var year = int.Parse(split[0]);
				var month = int.Parse(split[1]);
				var day = int.Parse(split[2]);
				return new DateOnly(year, month, day);
			}, kvp => kvp.Value);
			
			if (current.TryGetValue(member.Uuid, out var existing)) {
				existing.Active = true;
				existing.LeftAt = 0;
				
				existing.QuestParticipation = member.QuestParticipation;
				existing.Rank = member.Rank;
				existing.JoinedAt = member.Joined;

				foreach (var (date, xp) in xpHistory) {
					if (xp == 0) continue; // No point in saving zeros
					
					var found = existing.ExpHistory.FirstOrDefault(e => e.Day == date);
					if (found is not null) {
						found.Xp = xp;
						continue;
					}
					
					existing.ExpHistory.Add(new HypixelGuildMemberExp() {
						GuildMemberId = existing.Id,
						Day = date,
						Xp = xp
					});
				}
				continue;
			}

			var newMember = new HypixelGuildMember() {
				PlayerUuid = member.Uuid,
				GuildId = guild.Id,
				Active = true,
				Rank = member.Rank,
				JoinedAt = member.Joined,
				QuestParticipation = member.QuestParticipation,
				ExpHistory = xpHistory
					.Where(x => x.Value > 0)
					.Select(x => new HypixelGuildMemberExp {
						Day = x.Key,
						Xp = x.Value
					}).ToList()
			};
			
			context.HypixelGuildMembers.Add(newMember);
		}
		
		await context.SaveChangesAsync(c);

		var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
		var guildId = guild.Id;
		
		var newMemberUuids = newMembers.Select(n => n.Uuid).ToHashSet();
		var oldMemberUuids = guild.Members.Select(m => m.PlayerUuid).ToHashSet();
		
		var leftMemberUuids = oldMemberUuids.Except(newMemberUuids).ToList();
		var activeMemberUuids = newMemberUuids.ToList();
		
		// Update members who left the guild
		if (leftMemberUuids.Count != 0) {
			await context.HypixelGuildMembers
				.Where(m => m.GuildId == guildId && leftMemberUuids.Contains(m.PlayerUuid))
				.ExecuteUpdateAsync(s => s
						.SetProperty(e => e.Active, false)
						.SetProperty(e => e.LeftAt, now),
					cancellationToken: c);
		}
		
		// Flag deleted members and update last updated times
		if (activeMemberUuids.Count != 0) {
			// Update the GuildLastUpdated timestamp for all active members.
			await context.MinecraftAccounts
				.Where(a => activeMemberUuids.Contains(a.Id))
				.ExecuteUpdateAsync(s => s.SetProperty(e => e.GuildLastUpdated, now), 
					cancellationToken: c);

			// Make sure members are only active in one guild at a time
			await context.HypixelGuildMembers
				.Where(m => m.GuildId != guildId && activeMemberUuids.Contains(m.PlayerUuid))
				.ExecuteUpdateAsync(s => s
						.SetProperty(e => e.Active, false)
						.SetProperty(e => e.LeftAt, e => e.LeftAt == 0 ? now : e.LeftAt),
					cancellationToken: c);
		}
	}
	
	private async Task UpdateGuildByPlayerUuid(string playerUuid, CancellationToken c = default) {
		var guildResponse = await hypixelApi.FetchGuildByPlayerUuidAsync(playerUuid, c);
		var guild = guildResponse.Content?.Guild;
		
		if (!guildResponse.IsSuccessful || guild is null) {
			return;
		}
			
		await UpdateGuildInternal(guild, guild.Id, c);
	}
	
	public Task<List<HypixelGuildDetailsDto>> GetGuildListAsync(HypixelGuildListQuery query, CancellationToken c = default) {
		if (query.Collection is not null) {
			return GetGuildListByCollectionAsync(query, c);
		}
		
		if (query.Skill is not null) {
			return GetGuildListBySkillAsync(query, c);
		}
		
		return GetGuildListGeneralAsync(query, c);
	}
	
	public async Task<int> GetGuildLeaderboardTotalCount(HypixelGuildListQuery query, CancellationToken c = default) {
		var key = query.Collection ?? (query.Skill ?? query.SortBy.ToString());
		return await cache.GetOrCreateAsync($"hguilds_leaderboard_count_{key}", async (ct) => {
			if (query.Collection is not null) {
				var sql = """
				           SELECT COUNT(*)::int as "Value"
				           FROM "HypixelGuilds" g
				           INNER JOIN "HypixelGuildStats" s ON s."GuildId" = g."Id" AND s."IsLatest" = true
				           WHERE (s."Collections"->>@collectionId)::bigint IS NOT NULL
				           """;
				
				var result = await context.Database
					.SqlQueryRaw<int>(sql,
						new Npgsql.NpgsqlParameter("collectionId", query.Collection!))
					.FirstAsync(ct);
				
				return result;
			}
			
			if (query.Skill is not null) {
				var sql = """
				           SELECT COUNT(*)::int as "Value"
				           FROM "HypixelGuilds" g
				           INNER JOIN "HypixelGuildStats" s ON s."GuildId" = g."Id" AND s."IsLatest" = true
				           WHERE (s."Skills"->>@skillId)::bigint IS NOT NULL
				           """;
				
				var result = await context.Database
					.SqlQueryRaw<int>(sql,
						new Npgsql.NpgsqlParameter("skillId", query.Skill!))
					.FirstAsync(ct);
				
				return result;
			}
			
			var sortBy = query.SortBy;
			var statField = GetStatField(sortBy);
		
			if (sortBy == SortHypixelGuildsBy.MemberCount) {
				return await context.HypixelGuilds
					.Include(g => g.Stats.OrderByDescending(s => s.RecordedAt).Take(1))
					.Where(g => g.MemberCount >= 30)
					.CountAsync(ct);
			}
			
			var statSql = $"""
			           SELECT COUNT(*)::int as "Value"
			           FROM "HypixelGuilds" g
			           INNER JOIN LATERAL (
			           	SELECT *
			               FROM "HypixelGuildStats"
			               WHERE "GuildId" = g."Id"
			               ORDER BY "RecordedAt" DESC
			               LIMIT 1
			           ) AS stats ON true
			           WHERE g."MemberCount" >= 30 AND stats."{statField}" IS NOT NULL AND stats."{statField}" > 0
			           """;
		
			var count = await context.Database
				.SqlQueryRaw<int>(statSql)
				.FirstAsync(ct);
			
			return count;
		}, new HybridCacheEntryOptions {
			LocalCacheExpiration = TimeSpan.FromMinutes(5),
			Expiration = TimeSpan.FromMinutes(10)
		}, cancellationToken: c);
	}

	private async Task<List<HypixelGuildDetailsDto>> GetGuildListGeneralAsync(HypixelGuildListQuery query,
		CancellationToken c = default) 
	{
		var sortBy = query.SortBy;
		var statField = GetStatField(sortBy);
		var orderDirection = query.Descending ? "DESC" : "ASC";
		
		if (sortBy == SortHypixelGuildsBy.MemberCount) {
			var guildsQuery = context.HypixelGuilds
				.Include(g => g.Stats.OrderByDescending(s => s.RecordedAt).Take(1))
				.Where(g => g.MemberCount >= 30);
			
			guildsQuery = query.Descending
				? guildsQuery.OrderByDescending(g => g.MemberCount)
				: guildsQuery.OrderBy(g => g.MemberCount);
			
			var skip = (query.Page - 1) * query.PageSize;
			var guilds = await guildsQuery.Skip(skip).Take(query.PageSize).ToListAsync(c);
			
			return guilds.Select(g => g.ToDetailsDto()).ToList();
		}
		
		// For stats-based sorting, first get the ordered guild IDs, then load full entities with stats
		var sql = $"""
		           SELECT g."Id"
		           FROM "HypixelGuilds" g
		           INNER JOIN LATERAL (
		           	SELECT *
		               FROM "HypixelGuildStats"
		               WHERE "GuildId" = g."Id"
		               ORDER BY "RecordedAt" DESC
		               LIMIT 1
		           ) AS stats ON true
		           WHERE g."MemberCount" >= 30 AND stats."{statField}" IS NOT NULL AND stats."{statField}" > 0
		           ORDER BY stats."{statField}" {orderDirection}
		           LIMIT @pageSize OFFSET @offset;
		           """;
		
		var guildIdResults = await context.Database
			.SqlQueryRaw<GuildIdResult>(sql,
				new Npgsql.NpgsqlParameter("pageSize", query.PageSize),
				new Npgsql.NpgsqlParameter("offset", (query.Page - 1) * query.PageSize))
			.ToListAsync(c);
		
		var guildIds = guildIdResults.Select(r => r.Id).ToList();
		
		if (guildIds.Count == 0) {
			return [];
		}
		
		var guildsWithStats = await context.HypixelGuilds
			.Include(g => g.Stats.OrderByDescending(s => s.RecordedAt).Take(1))
			.Where(g => guildIds.Contains(g.Id))
			.ToListAsync(c);
		
		var orderedGuilds = guildIds
			.Select(id => guildsWithStats.FirstOrDefault(g => g.Id == id))
			.Where(g => g != null)
			.Select(g => g!.ToDetailsDto())
			.ToList();
		
		return orderedGuilds;
	}

	private static string GetStatField(SortHypixelGuildsBy sortBy) {
		return sortBy switch {
			SortHypixelGuildsBy.SkyblockExperience => "SkyblockExperience_Total",
			SortHypixelGuildsBy.SkyblockExperienceAverage => "SkyblockExperience_Average",
			SortHypixelGuildsBy.SkillLevel => "SkillLevel_Total",
			SortHypixelGuildsBy.SkillLevelAverage => "SkillLevel_Average",
			SortHypixelGuildsBy.HypixelLevelAverage => "HypixelLevel_Average",
			SortHypixelGuildsBy.SlayerExperience => "SlayerExperience_Total",
			SortHypixelGuildsBy.CatacombsExperience => "CatacombsExperience_Total",
			SortHypixelGuildsBy.FarmingWeight => "FarmingWeight_Total",
			SortHypixelGuildsBy.Networth => "Networth_Total",
			SortHypixelGuildsBy.NetworthAverage => "Networth_Average",
			_ => "SkyblockExperience_Average"
		};
	}

	private async Task<List<HypixelGuildDetailsDto>> GetGuildListByCollectionAsync(HypixelGuildListQuery query,
		CancellationToken c = default) 
	{
		var sql = $"""
		           SELECT (s."Collections"->>@collectionId)::bigint as "Amount", g."Id", g."Name", g."CreatedAt", g."Tag", g."TagColor", g."MemberCount", g."LastUpdated"
		           FROM "HypixelGuilds" g
		           INNER JOIN "HypixelGuildStats" s ON s."GuildId" = g."Id" AND s."IsLatest" = true
		           WHERE (s."Collections"->>@collectionId)::bigint IS NOT NULL
		           ORDER BY (s."Collections"->>@collectionId)::bigint DESC
		           LIMIT @pageSize OFFSET @offset;
		           """;
		
		var results = await context.Set<HypixelGuildLeaderboardResult>()
			.FromSqlRaw(sql,
				new Npgsql.NpgsqlParameter("collectionId", query.Collection!),
				new Npgsql.NpgsqlParameter("pageSize", query.PageSize),
				new Npgsql.NpgsqlParameter("offset", (query.Page - 1) * query.PageSize))
			.ToListAsync(c);
		
		return results.Select(r => r.ToDto()).ToList();
	}
	
	private async Task<List<HypixelGuildDetailsDto>> GetGuildListBySkillAsync(HypixelGuildListQuery query,
		CancellationToken c = default) 
	{
		var sql = $"""
		           SELECT (s."Skills"->>@skillId)::bigint as "Amount", g."Id", g."Name", g."CreatedAt", g."Tag", g."TagColor", g."MemberCount", g."LastUpdated"
		           FROM "HypixelGuilds" g
		           INNER JOIN "HypixelGuildStats" s ON s."GuildId" = g."Id" AND s."IsLatest" = true
		           WHERE (s."Skills"->>@skillId)::bigint IS NOT NULL
		           ORDER BY (s."Skills"->>@skillId)::bigint DESC
		           LIMIT @pageSize OFFSET @offset;
		           """;
		
		var results = await context.Set<HypixelGuildLeaderboardResult>()
			.FromSqlRaw(sql,
				new Npgsql.NpgsqlParameter("skillId", query.Skill!),
				new Npgsql.NpgsqlParameter("pageSize", query.PageSize),
				new Npgsql.NpgsqlParameter("offset", (query.Page - 1) * query.PageSize))
			.ToListAsync(c);
		
		return results.Select(r => r.ToDto()).ToList();
	}

	public async Task<IReadOnlyList<HypixelGuildSearchResultDto>> SearchGuildsAsync(string query, int limit,
		CancellationToken c = default) {
		if (string.IsNullOrWhiteSpace(query)) {
			return [];
		}

		var normalized = query.Trim().ToLowerInvariant();
		var cappedLimit = Math.Clamp(limit, 1, 50);
		const int directBatchSize = 250;
		const int fallbackBatchSize = 1000;

		var guilds = context.HypixelGuilds
			.AsNoTracking()
			.Where(g => g.MemberCount > 0);

		static string EscapeLike(string value) {
			return value
				.Replace("\\", "\\\\", StringComparison.Ordinal)
				.Replace("%", "\\%", StringComparison.Ordinal)
				.Replace("_", "\\_", StringComparison.Ordinal);
		}

		var pattern = $"%{EscapeLike(normalized)}%";

		var directMatches = await guilds
			.Where(g => EF.Functions.ILike(g.NameLower, pattern))
			.OrderBy(g => g.NameLower)
			.Take(directBatchSize)
			.Select(g => new HypixelGuildSearchCandidate {
				Id = g.Id,
				Name = g.Name,
				NameLower = g.NameLower,
				MemberCount = g.MemberCount,
				Tag = g.Tag,
				TagColor = g.TagColor
			})
			.ToListAsync(c);

		var candidates = new Dictionary<string, HypixelGuildSearchCandidate>(StringComparer.OrdinalIgnoreCase);
		foreach (var candidate in directMatches) {
			candidates.TryAdd(candidate.Id, candidate);
		}

		if (candidates.Count < directBatchSize) {
			var fallback = await guilds
				.OrderByDescending(g => g.MemberCount)
				.ThenBy(g => g.NameLower)
				.Take(fallbackBatchSize)
				.Select(g => new HypixelGuildSearchCandidate {
					Id = g.Id,
					Name = g.Name,
					NameLower = g.NameLower,
					MemberCount = g.MemberCount,
					Tag = g.Tag,
					TagColor = g.TagColor
				})
				.ToListAsync(c);

			foreach (var candidate in fallback) {
				candidates.TryAdd(candidate.Id, candidate);
			}
		}

		if (candidates.Count == 0) {
			return [];
		}

		foreach (var candidate in candidates.Values) {
			var score = ComputeNormalizedSimilarity(normalized, candidate.NameLower);
			if (candidate.NameLower.Contains(normalized, StringComparison.Ordinal)) {
				score = Math.Max(score, 0.95);
			}
			candidate.Score = score;
		}

		var ordered = candidates.Values
			.OrderByDescending(a => a.Score)
			.ThenByDescending(a => a.MemberCount)
			.ThenBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
			.ToList();

		var scoreFloor = normalized.Length switch {
			<= 3 => 0.35,
			<= 5 => 0.25,
			_ => 0.2
		};

		if (ordered.Count > cappedLimit) {
			var filtered = ordered.Where(a => a.Score >= scoreFloor).ToList();
			if (filtered.Count > 0) {
				ordered = filtered;
			}
		}

		return ordered
			.Take(cappedLimit)
			.Select(ToDto)
			.ToList();
	}

	public async Task<(int rank, double amount)> GetGuildRankAsync(string guildId, SortHypixelGuildsBy? sortBy, 
		string? collection, string? skill, CancellationToken c = default) {
		
		if (collection is not null) {
			return await GetGuildCollectionRankAsync(guildId, collection, c);
		}
		
		if (skill is not null) {
			return await GetGuildSkillRankAsync(guildId, skill, c);
		}
		
		var sort = sortBy ?? SortHypixelGuildsBy.SkyblockExperienceAverage;
		return await GetGuildGeneralRankAsync(guildId, sort, c);
	}

	private async Task<(int rank, double amount)> GetGuildCollectionRankAsync(string guildId, string collectionId, CancellationToken c) {
		var sql = """
			WITH ranked_guilds AS (
				SELECT 
					g."Id",
					COALESCE((stats."Collections"->>@collectionId)::bigint, 0) as amount,
					ROW_NUMBER() OVER (ORDER BY COALESCE((stats."Collections"->>@collectionId)::bigint, 0) DESC) as rank
				FROM "HypixelGuilds" g
				LEFT JOIN LATERAL (
					SELECT "Collections"
					FROM "HypixelGuildStats"
					WHERE "GuildId" = g."Id"
					  AND "Collections"->>@collectionId IS NOT NULL
					ORDER BY "RecordedAt" DESC
					LIMIT 1
				) stats ON true
				WHERE (stats."Collections"->>@collectionId) IS NOT NULL
			)
			SELECT rank::int as "Rank", amount::float as "Amount"
			FROM ranked_guilds WHERE "Id" = @guildId
		""";

		var result = await context.Database
			.SqlQueryRaw<GuildRankResult>(sql,
				new Npgsql.NpgsqlParameter("collectionId", collectionId),
				new Npgsql.NpgsqlParameter("guildId", guildId))
			.FirstOrDefaultAsync(c);

		return result is not null ? (result.Rank, result.Amount) : (0, 0);
	}

	private async Task<(int rank, double amount)> GetGuildSkillRankAsync(string guildId, string skillId, CancellationToken c) {
		var sql = """
			WITH ranked_guilds AS (
				SELECT 
					g."Id",
					COALESCE((stats."Skills"->>@skillId)::bigint, 0) as amount,
					ROW_NUMBER() OVER (ORDER BY COALESCE((stats."Skills"->>@skillId)::bigint, 0) DESC) as rank
				FROM "HypixelGuilds" g
				LEFT JOIN LATERAL (
					SELECT "Skills"
					FROM "HypixelGuildStats"
					WHERE "GuildId" = g."Id"
					  AND "Skills"->>@skillId IS NOT NULL
					ORDER BY "RecordedAt" DESC
					LIMIT 1
				) stats ON true
				WHERE (stats."Skills"->>@skillId) IS NOT NULL
			)
			SELECT rank::int as "Rank", amount::float as "Amount"
			FROM ranked_guilds WHERE "Id" = @guildId
		""";

		var result = await context.Database
			.SqlQueryRaw<GuildRankResult>(sql,
				new Npgsql.NpgsqlParameter("skillId", skillId),
				new Npgsql.NpgsqlParameter("guildId", guildId))
			.FirstOrDefaultAsync(c);

		return result is not null ? (result.Rank, result.Amount) : (0, 0);
	}

	private async Task<(int rank, double amount)> GetGuildGeneralRankAsync(string guildId, SortHypixelGuildsBy sortBy, CancellationToken c) {
		// Build the ORDER BY expression using the same logic as GetGuildListAsync
		var (orderColumn, useStats) = sortBy switch {
			SortHypixelGuildsBy.MemberCount => ("g.\"MemberCount\"", false),
			SortHypixelGuildsBy.SkyblockExperience => ("stats.\"SkyblockExperience_Total\"", true),
			SortHypixelGuildsBy.SkyblockExperienceAverage => ("stats.\"SkyblockExperience_Average\"", true),
			SortHypixelGuildsBy.SkillLevel => ("stats.\"SkillLevel_Total\"", true),
			SortHypixelGuildsBy.SkillLevelAverage => ("stats.\"SkillLevel_Average\"", true),
			SortHypixelGuildsBy.HypixelLevelAverage => ("stats.\"HypixelLevel_Average\"", true),
			SortHypixelGuildsBy.SlayerExperience => ("stats.\"SlayerExperience_Total\"", true),
			SortHypixelGuildsBy.CatacombsExperience => ("stats.\"CatacombsExperience_Total\"", true),
			SortHypixelGuildsBy.FarmingWeight => ("stats.\"FarmingWeight_Total\"", true),
			SortHypixelGuildsBy.Networth => ("stats.\"Networth_Total\"", true),
			SortHypixelGuildsBy.NetworthAverage => ("stats.\"Networth_Average\"", true),
			_ => ("stats.\"SkyblockExperience_Average\"", true)
		};

		var whereClause = useStats ? "WHERE stats.\"Id\" IS NOT NULL AND g.\"MemberCount\" >= 30" : "WHERE g.\"MemberCount\" >= 30";

		var sql = $"""
			WITH ranked_guilds AS (
				SELECT 
					g."Id",
					COALESCE({orderColumn}, 0) as amount,
					ROW_NUMBER() OVER (ORDER BY COALESCE({orderColumn}, 0) DESC) as rank
				FROM "HypixelGuilds" g
				LEFT JOIN LATERAL (
					SELECT *
					FROM "HypixelGuildStats"
					WHERE "GuildId" = g."Id"
					ORDER BY "RecordedAt" DESC
					LIMIT 1
				) stats ON true
				{whereClause}
			)
			SELECT rank::int as "Rank", amount::float as "Amount"
			FROM ranked_guilds WHERE "Id" = @guildId
		""";

		var result = await context.Database
			.SqlQueryRaw<GuildRankResult>(sql,
				new Npgsql.NpgsqlParameter("guildId", guildId))
			.FirstOrDefaultAsync(c);

		return result is not null ? (result.Rank, result.Amount) : (0, 0);
	}

	private static HypixelGuildSearchResultDto ToDto(HypixelGuildSearchCandidate candidate) => new() {
		Id = candidate.Id,
		Name = candidate.Name,
		MemberCount = candidate.MemberCount,
		Tag = candidate.Tag,
		TagColor = candidate.TagColor
	};

	private static double ComputeNormalizedSimilarity(string left, string right) {
		if (string.IsNullOrEmpty(right)) return 0d;

		var distance = FormatUtils.LevenshteinDistance(left, right);
		var maxLength = Math.Max(left.Length, right.Length);
		return maxLength == 0 ? 1d : 1d - (double)distance / maxLength;
	}

	private static int LevenshteinDistance(string left, string right) {
		if (string.IsNullOrEmpty(right)) return 0;

		var distance = FormatUtils.LevenshteinDistance(left, right);
		var maxLength = Math.Max(left.Length, right.Length);
		return maxLength == 0 ? 0 : (int)Math.Round((double)distance / maxLength, 4);
	}

	private sealed class HypixelGuildSearchCandidate
	{
		public required string Id { get; init; }
		public required string Name { get; init; }
		public required string NameLower { get; init; }
		public int MemberCount { get; init; }
		public string? Tag { get; init; }
		public string? TagColor { get; init; }
		public double Score { get; set; }
	}
}

public class GuildRankResult
{
	public int Rank { get; set; }
	public double Amount { get; set; }
}

public class GuildRankResultEntityConfiguration : IEntityTypeConfiguration<GuildRankResult>
{
	public void Configure(EntityTypeBuilder<GuildRankResult> builder) {
		builder.HasNoKey();
		builder.ToTable("GuildRankResult", t => t.ExcludeFromMigrations());
	}
}

public class GuildIdResult
{
	public required string Id { get; set; }
}

public class GuildIdResultEntityConfiguration : IEntityTypeConfiguration<GuildIdResult>
{
	public void Configure(EntityTypeBuilder<GuildIdResult> builder) {
		builder.HasNoKey();
		builder.ToTable("GuildIdResult", t => t.ExcludeFromMigrations());
	}
}

