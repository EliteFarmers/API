using EliteAPI.Configuration.Settings;
using EliteAPI.Data;
using EliteAPI.Features.Account.Models;
using EliteAPI.Features.HypixelGuilds.Models;
using EliteAPI.Services.Interfaces;
using EliteAPI.Utilities;
using EliteFarmers.HypixelAPI;
using EliteFarmers.HypixelAPI.DTOs;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;

namespace EliteAPI.Features.HypixelGuilds.Services;

public interface IHypixelGuildService
{
	Task UpdateGuildIfNeeded(MinecraftAccount account, CancellationToken c = default);
	Task<List<HypixelGuildDetailsDto>> GetGuildListAsync(HypixelGuildListQuery query, CancellationToken c = default);
	Task<IReadOnlyList<HypixelGuildSearchResultDto>> SearchGuildsAsync(string query, int limit,
        CancellationToken c = default);
}

[RegisterService<IHypixelGuildService>(LifeTime.Scoped)]
public class HypixelGuildService(
	DataContext context,
	IHttpContextAccessor httpContextAccessor,
	HybridCache hybridCache,
	IMojangService mojangService,
	ILogger<HypixelGuildService> logger,
	IHypixelApi hypixelApi,
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

				// Check that the player is decently active first
				// Hypixel network level ~124
				if (hypixelXp > 20_000_000) {
					await UpdateGuildByPlayerUuid(account.Id, c);
				}
			}
		} catch (Exception e) {
			logger.LogError(e, "Failed to update guild");
		}
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
		
			if (!guildResponse.IsSuccessful || guild is null) {
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

		await UpdateGuildMembers(existing, guild.Members, c);
		await context.SaveChangesAsync(c);
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

		var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
		
		// Flag deleted members and update last updated times
		foreach (var member in guild.Members) {
			var stillExists = newMembers.FirstOrDefault(n => n.Uuid == member.PlayerUuid);
			if (stillExists is null) {
				member.LeftAt = now;
				member.Active = false;
			} else {
				member.Active = true;
				member.LeftAt = 0;
				await context.MinecraftAccounts
					.Where(a => member.PlayerUuid == a.Id)
					.ExecuteUpdateAsync(a => 
						a.SetProperty(e => e.GuildLastUpdated, now), cancellationToken: c);
			}
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
		
		var guildsQuery = context.HypixelGuilds
			.Include(g => g.Stats
				.OrderByDescending(s => s.RecordedAt)
				.Take(1))
			.Where(g => g.Stats.Count > 0 && g.MemberCount > 30)
			.AsQueryable();

		guildsQuery = query.SortBy switch {
			SortHypixelGuildsBy.MemberCount => query.Descending
				? guildsQuery.OrderByDescending(g => g.MemberCount)
				: guildsQuery.OrderBy(g => g.MemberCount),
			SortHypixelGuildsBy.SkyblockExperienceAverage => query.Descending
				? guildsQuery.OrderByDescending(g => g.Stats.First().SkyblockExperience.Average)
				: guildsQuery.OrderBy(g => g.Stats.First().SkyblockExperience.Average),
			SortHypixelGuildsBy.SkyblockExperience => query.Descending
				? guildsQuery.OrderByDescending(g => g.Stats.First().SkyblockExperience.Total)
				: guildsQuery.OrderBy(g => g.Stats.First().SkyblockExperience.Total),
			SortHypixelGuildsBy.SkillLevelAverage => query.Descending
				? guildsQuery.OrderByDescending(g => g.Stats.First().SkillLevel.Average)
				: guildsQuery.OrderBy(g => g.Stats.First().SkillLevel.Average),
			SortHypixelGuildsBy.SkillLevel => query.Descending
				? guildsQuery.OrderByDescending(g => g.Stats.First().SkillLevel.Total)
				: guildsQuery.OrderBy(g => g.Stats.First().SkillLevel.Total),
			SortHypixelGuildsBy.HypixelLevelAverage => query.Descending
				? guildsQuery.OrderByDescending(g => g.Stats.First().HypixelLevel.Average)
				: guildsQuery.OrderBy(g => g.Stats.First().HypixelLevel.Average),
			SortHypixelGuildsBy.SlayerExperience => query.Descending
				? guildsQuery.OrderByDescending(g => g.Stats.First().SlayerExperience.Total)
				: guildsQuery.OrderBy(g => g.Stats.First().SlayerExperience.Total),
			SortHypixelGuildsBy.CatacombsExperience => query.Descending
				? guildsQuery.OrderByDescending(g => g.Stats.First().CatacombsExperience.Total)
				: guildsQuery.OrderBy(g => g.Stats.First().CatacombsExperience.Total),
			SortHypixelGuildsBy.FarmingWeight => query.Descending
				? guildsQuery.OrderByDescending(g => g.Stats.First().FarmingWeight.Total)
				: guildsQuery.OrderBy(g => g.Stats.First().FarmingWeight.Total),
			SortHypixelGuildsBy.Networth => query.Descending
				? guildsQuery.OrderByDescending(g => g.Stats.First().Networth.Total)
				: guildsQuery.OrderBy(g => g.Stats.First().Networth.Total),
			SortHypixelGuildsBy.NetworthAverage => query.Descending
				? guildsQuery.OrderByDescending(g => g.Stats.First().Networth.Average)
				: guildsQuery.OrderBy(g => g.Stats.First().Networth.Average),
			_ => guildsQuery.OrderByDescending(g => g.Stats.First().SkyblockExperience.Average),
		};

		var skip = (query.Page - 1) * query.PageSize;

		return guildsQuery
			.Skip(skip)
			.Take(query.PageSize)
			.SelectDetailsDto()
			.ToListAsync(c);
	}

	private async Task<List<HypixelGuildDetailsDto>> GetGuildListByCollectionAsync(HypixelGuildListQuery query,
		CancellationToken c = default) 
	{
		var sql = $"""
		           SELECT c."Amount", g."Id", g."Name", g."CreatedAt", g."Tag", g."TagColor", g."MemberCount", g."LastUpdated"
		           FROM "HypixelGuilds" g
		           LEFT JOIN (
		           	SELECT DISTINCT ON ("GuildId")
		           		"GuildId",
		                   ("Collections"->>@collectionId)::bigint as "Amount"
		               FROM "HypixelGuildStats"
		               WHERE "Collections"->>@collectionId IS NOT NULL
		               ORDER BY "GuildId", "RecordedAt" DESC
		           ) AS c ON c."GuildId" = g."Id"
		           WHERE c."Amount" IS NOT NULL
		           ORDER BY c."Amount"::bigint DESC
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
		           SELECT c."Amount", g."Id", g."Name", g."CreatedAt", g."Tag", g."TagColor", g."MemberCount", g."LastUpdated"
		           FROM "HypixelGuilds" g
		           LEFT JOIN (
		           	SELECT DISTINCT ON ("GuildId")
		           		"GuildId",
		                   ("Skills"->>@skillId)::bigint as "Amount"
		               FROM "HypixelGuildStats"
		               WHERE "Skills"->>@skillId IS NOT NULL
		               ORDER BY "GuildId", "RecordedAt" DESC
		           ) AS c ON c."GuildId" = g."Id"
		           WHERE c."Amount" IS NOT NULL
		           ORDER BY c."Amount"::bigint DESC
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
