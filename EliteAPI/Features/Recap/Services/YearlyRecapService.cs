using EliteAPI.Data;
using EliteAPI.Features.Leaderboards.Services;
using EliteAPI.Features.Profiles.Services;
using EliteAPI.Features.Recap.Commands;
using EliteAPI.Features.Recap.Models;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Models.Entities.Timescale;
using EliteAPI.Parsers.Farming;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Recap.Services;

public interface IYearlyRecapService
{
	Task<YearlyRecapDto?> GetRecapAsync(string playerUuid, string profileUuid, int year);
	Task<YearlyRecapSnapshot?> GenerateGlobalStatsAsync(int year);
	Task<bool> TogglePublicStatusAsync(string playerUuid, string profileUuid, int year, bool isPublic);
	Task<bool> IsPublicRecapAsync(string playerUuid, string profileUuid, int year);
	bool ValidYear(int year);
}

[RegisterService<IYearlyRecapService>(LifeTime.Scoped)]
public class YearlyRecapService(DataContext context, ILbService lbService, IMemberService memberService)
	: IYearlyRecapService
{
	private static readonly string[] CropColumns = [
		"Wheat", "Carrot", "Potato", "Pumpkin", "Melon", "Mushroom",
		"CocoaBeans", "Cactus", "SugarCane", "NetherWart", "Seeds"
	];

	private static readonly string[] PestColumns = [
		"Beetle", "Cricket", "Fly", "Locust", "Mite", "Mosquito",
		"Moth", "Rat", "Slug", "Earthworm", "Mouse"
	];

	private static readonly string[] SkillColumns = [
		"Farming", "Mining", "Combat", "Foraging", "Fishing",
		"Enchanting", "Alchemy", "Carpentry", "Runecrafting",
		"Social", "Taming"
	];

	public async Task<YearlyRecapDto?> GetRecapAsync(string playerUuid, string profileUuid, int year) {
		var profileMember = await context.ProfileMembers
			.Include(pm => pm.Profile)
			.Include(pm => pm.MinecraftAccount)
			.ThenInclude(ma => ma.EliteAccount)
			.Include(pm => pm.Farming)
			.FirstOrDefaultAsync(pm => pm.PlayerUuid == playerUuid && pm.ProfileId == profileUuid);

		if (profileMember == null)
			return null;

		// Check for Global Stats
		var globalStats = await context.YearlyRecapSnapshots.FindAsync(year);

		if (globalStats == null) {
			await new GenerateGlobalStatsCommand { Year = year }.QueueJobAsync();
			
			throw new InvalidOperationException(
				$"Global stats for {year} are being generated. Please try again in few minutes.");
		}

		var existingRecap = await context.YearlyRecaps
			.FirstOrDefaultAsync(r => r.ProfileMemberId == profileMember.Id && r.Year == year);

		if (existingRecap != null) {
			existingRecap.Views++;
			await context.SaveChangesAsync();
			return YearlyRecapDtoMapper.FromYearlyRecap(existingRecap, globalStats.Data);
		}

		var recapData = await GeneratePlayerRecapDataAsync(profileMember, year, globalStats.Data);

		var newRecap = new YearlyRecap {
			ProfileMemberId = profileMember.Id,
			ProfileMember = profileMember,
			Year = year,
			Data = recapData
		};

		context.YearlyRecaps.Add(newRecap);
		await context.SaveChangesAsync();

		return YearlyRecapDtoMapper.FromYearlyRecap(newRecap, globalStats.Data);
	}

	public async Task<YearlyRecapSnapshot?> GenerateGlobalStatsAsync(int year) {
		context.Database.SetCommandTimeout(TimeSpan.FromMinutes(30));

		var startDate = new DateTimeOffset(new DateTime(year - 1, 12, 7, 0, 0, 0, DateTimeKind.Utc));
		var endDate = new DateTimeOffset(new DateTime(year, 12, 7, 0, 0, 0, DateTimeKind.Utc));
		var startTs = startDate.ToUnixTimeSeconds();
		var endTs = endDate.ToUnixTimeSeconds();

		var trackedPlayers = await context.ProfileMembers.Select(pm => pm.PlayerUuid).Distinct().CountAsync();

		var bannedWiped = await context.ProfileMembers
			.Include(pm => pm.Profile)
			.Where(pm => pm.WasRemoved && pm.LastUpdated >= startTs && pm.LastUpdated <= endTs && pm.Profile.GameMode != "bingo")
			.CountAsync();

		var ironmanToNormal = await context.GameModeHistories
			.Where(h => h.Old == "ironman" && h.ChangedAt >= startDate && h.ChangedAt <= endDate)
			.CountAsync();

		var cropsMedian = await GetGlobalBreakdownAsync<long>("CropCollections", CropColumns, startDate, endDate, true);
		var pestsMedian = await GetGlobalBreakdownAsync<long>("CropCollections", PestColumns, startDate, endDate, true);
		var skillsMedian =
			await GetGlobalBreakdownAsync<double>("SkillExperiences", SkillColumns, startDate, endDate, true);

		var cropsSum = await GetGlobalBreakdownAsync<long>("CropCollections", CropColumns, startDate, endDate, false);
		var pestsSum = await GetGlobalBreakdownAsync<long>("CropCollections", PestColumns, startDate, endDate, false);
		var skillsSum =
			await GetGlobalBreakdownAsync<double>("SkillExperiences", SkillColumns, startDate, endDate, false);

		var totalFarmingWeight =
			await context.ProfileMembers.Include(pm => pm.Farming).SumAsync(pm => pm.Farming.TotalWeight);

		var globalRecap = new GlobalRecap {
			TrackedPlayers = trackedPlayers,
			BannedWiped = bannedWiped,
			IronmanToNormal = ironmanToNormal,

			TotalCrops = cropsSum.Values.Sum(),
			TotalXp = skillsSum.Values.Sum(),
			TotalPests = pestsSum.Values.Sum(),
			TotalFarmingWeight = totalFarmingWeight,

			Crops = cropsMedian,
			Skills = skillsMedian,
			Pests = pestsMedian,

			TotalCropsBreakdown = cropsSum,
			TotalSkillsBreakdown = skillsSum,
			TotalPestsBreakdown = pestsSum
		};

		var snapshot = new YearlyRecapSnapshot {
			Year = year,
			Data = globalRecap
		};

		var existing = await context.YearlyRecapSnapshots.FindAsync(year);
		if (existing != null) {
			existing.Data = globalRecap;
		}
		else {
			context.YearlyRecapSnapshots.Add(snapshot);
		}

		await context.SaveChangesAsync();
		return snapshot;
	}

	private async Task<Dictionary<string, T>> GetGlobalBreakdownAsync<T>(string tableName, string[] columns,
		DateTimeOffset start, DateTimeOffset end, bool useMedian) where T : struct {
		var sql = BuildGlobalBreakdownSql(tableName, columns, start, end, useMedian);
		var dict = new Dictionary<string, T>();

		var connection = context.Database.GetDbConnection();
		await context.Database.OpenConnectionAsync();

		await using var command = connection.CreateCommand();
		command.CommandText = sql;
		command.CommandTimeout = 1800;

		await using var reader = await command.ExecuteReaderAsync();
		if (!await reader.ReadAsync()) return dict;

		for (var i = 0; i < reader.FieldCount; i++) {
			var name = reader.GetName(i);
			var value = reader.GetValue(i);
			if (value != DBNull.Value) {
				dict[name] = (T)Convert.ChangeType(value, typeof(T));
			}
			else {
				dict[name] = default;
			}
		}

		return dict;
	}

	private static string BuildGlobalBreakdownSql(string tableName, string[] columns, DateTimeOffset start,
		DateTimeOffset end,
		bool useMedian) {
		string selectClause;

		if (useMedian) {
			selectClause = string.Join(", ",
				columns.Select(c =>
					$"PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY NULLIF(\"last_{c}\" - \"first_{c}\", 0)) as \"{c}\""));
		}
		else {
			selectClause = string.Join(", ",
				columns.Select(c => $"COALESCE(SUM(\"last_{c}\" - \"first_{c}\"), 0) as \"{c}\""));
		}

		var firstSelectors = string.Join(", ",
			columns.Select(c =>
				$"FIRST_VALUE(\"{c}\") OVER (PARTITION BY \"ProfileMemberId\" ORDER BY \"Time\" ASC) as \"first_{c}\""));
		var lastSelectors = string.Join(", ",
			columns.Select(c =>
				$"LAST_VALUE(\"{c}\") OVER (PARTITION BY \"ProfileMemberId\" ORDER BY \"Time\" ASC ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING) as \"last_{c}\""));
		var outerSelectors = string.Join(", ", columns.Select(c => $"\"first_{c}\", \"last_{c}\""));

		// Filter out rows where all columns are 0
		var nonZeroFilter = string.Join(" OR ", columns.Select(c => $"\"{c}\" != 0"));

		return $@"
            WITH range_data AS (
                SELECT 
                    ""ProfileMemberId"", 
                    {firstSelectors},
                    {lastSelectors}
                FROM ""{tableName}""
                WHERE ""Time"" >= '{start:O}' AND ""Time"" <= '{end:O}'
                AND ({nonZeroFilter})
            )
            SELECT {selectClause}
            FROM (
                SELECT DISTINCT ""ProfileMemberId"", {outerSelectors} 
                FROM range_data
            ) as distinct_ranges
        ";
	}

	private async Task<YearlyRecapData> GeneratePlayerRecapDataAsync(ProfileMember member, int year,
		GlobalRecap globalStats) {
		var startDate = new DateTimeOffset(new DateTime(year - 1, 12, 7, 0, 0, 0, DateTimeKind.Utc));
		var endDate = new DateTimeOffset(new DateTime(year, 12, 7, 0, 0, 0, DateTimeKind.Utc));
		var startTs = startDate.ToUnixTimeSeconds();
		var endTs = endDate.ToUnixTimeSeconds();

		var data = new YearlyRecapData {
			Year = year.ToString(),
			CurrentProfile = member.ProfileId,
			Player = {
				Ign = member.MinecraftAccount.Name,
				Uuid = member.MinecraftAccount.Id
			}
		};
		
		// Fetch timestamp and total XP to check for activity
		var skillData = await context.SkillExperiences
			.Where(s => s.ProfileMemberId == member.Id && s.Time >= startDate && s.Time <= endDate)
			.Select(s => new {
				s.Time,
				TotalXp = s.Farming + s.Mining + s.Combat + s.Foraging + s.Fishing + s.Enchanting + s.Alchemy +
				          s.Carpentry + s.Runecrafting + s.Social + s.Taming
			})
			.OrderBy(s => s.Time)
			.ToListAsync();

		var activeDays = new List<DateTime>();
		if (skillData.Count != 0) {
			var days = skillData.GroupBy(d => d.Time.UtcDateTime.Date);
			foreach (var day in days) {
				var min = day.Min(x => x.TotalXp);
				var max = day.Max(x => x.TotalXp);
				if (max > min) {
					activeDays.Add(day.Key);
				}
			}

			data.Player.DaysActive = activeDays.Count;

			if (activeDays.Count != 0) {
				data.Player.MostActiveMonth = activeDays
					.GroupBy(d => d.ToString("MMMM"))
					.OrderByDescending(g => g.Count())
					.First().Key;
			}

			data.Player.FirstDataPoint = skillData.Min(x => x.Time).ToString("O");
			data.Player.LastDataPoint = skillData.Max(x => x.Time).ToString("O");
		}
		else {
			data.Player.DaysActive = 0;
		}

		// Farming Weight
		data.Player.FarmingWeight.Total = member.Farming.TotalWeight;
		if (globalStats is { TrackedPlayers: > 0, TotalFarmingWeight: > 0 }) {
			var averageWeight = globalStats.TotalFarmingWeight / globalStats.TrackedPlayers;
			data.Player.FarmingWeight.AverageComparison =
				Sanitize(averageWeight > 0 ? member.Farming.TotalWeight / averageWeight : 0);
		}

		// Profiles
		var profiles = await context.ProfileMembers
			.Where(pm => pm.PlayerUuid == member.PlayerUuid)
			.Include(pm => pm.Profile)
			.Include(pm => pm.Farming)
			.ToListAsync();

		data.Profiles = profiles.Select(p => new ProfileRecapInfo {
			Name = p.Profile.ProfileName,
			CuteName = p.ProfileName ?? p.Profile.ProfileName,
			IsMain = p.IsSelected,
			Wiped = p.WasRemoved
		}).ToList();

		data.AllProfilesSummary.TotalCoinsGained = profiles.Sum(p => p.Networth);
		data.AllProfilesSummary.TotalWeightGained = profiles.Sum(p => p.Farming.TotalWeight);
		data.AllProfilesSummary.WipedProfiles = profiles.Count(p => p.WasRemoved);

		// Contests
		var contests = await context.ContestParticipations
			.Where(c => c.ProfileMemberId == member.Id && c.JacobContest.Timestamp >= startTs &&
			            c.JacobContest.Timestamp <= endTs)
			.Include(c => c.JacobContest)
			.ToListAsync();

		data.Contests.Total = contests.Count;
		data.Contests.PerCrop = contests
			.GroupBy(c => c.JacobContest.Crop)
			.ToDictionary(g => g.Key.ToString(), g => g.Count());

		data.Contests.HighestPlacements = contests
			.Where(x => x.Position > -1 && x.MedalEarned != ContestMedal.Unclaimable)
			.OrderBy(x => x.Position)
			.ThenByDescending(x => x.Collected)
			.Select(g => new ContestPlacementRecap {
					Crop = g.JacobContest.Crop.ToString(),
					Rank = g.Position,
					Medal = g.MedalEarned.ToString()
				})
			.ToList();

		// Streak Calculation
		var cropData = await context.CropCollections
			.Where(c => c.ProfileMemberId == member.Id && c.Time >= startDate && c.Time <= endDate)
			.OrderBy(c => c.Time)
			.Select(c => new {
				c.Time,
				Total = c.Wheat + c.Carrot + c.Potato + c.Pumpkin + c.Melon + c.Mushroom + c.CocoaBeans + c.Cactus +
				        c.SugarCane + c.NetherWart + c.Seeds
			})
			.ToListAsync();

		data.Streak = CalculateStreak(contests, cropData.Select(c => (c.Time, c.Total)).ToList());

		// Events
		var events = await context.EventMembers
			.Include(em => em.Event)
			.ThenInclude(e => e.Banner)
			.Where(em => em.ProfileMemberId == member.Id && em.StartTime >= startDate && em.StartTime <= endDate)
			.ToListAsync();

		foreach (var e in events) {
			var rank = await context.EventMembers
				.CountAsync(em => em.EventId == e.EventId && em.Score > e.Score) + 1;

			data.Events.Add(new EventRecap {
				Name = e.Event.Name,
				Type = e.Event.Type.ToString(),
				Participated = true,
				Rank = rank,
				Score = e.Score,
				Banner = e.Event.Banner?.Path
			});
		}

		if (member.MinecraftAccount.AccountId.HasValue) {
			var hasPurchased = await context.ProductAccesses
				.Include(pa => pa.Product)
				.Where(pa => pa.UserId == member.MinecraftAccount.AccountId && pa.Product.Price > 0)
				.AnyAsync(pa =>
					!pa.Revoked &&
					pa.StartDate <= endDate &&
					(pa.EndDate == null || pa.EndDate >= startDate)
				);
			data.Shop.HasPurchased = hasPurchased;
		}

		var firstCrops = await context.CropCollections
			.Where(c => c.ProfileMemberId == member.Id && c.Time >= startDate && c.Time <= endDate)
			.Where(c => c.Wheat > 0 || c.Carrot > 0 || c.Potato > 0 || c.Pumpkin > 0 || c.Melon > 0 || c.Mushroom > 0 ||
			            c.CocoaBeans > 0 || c.Cactus > 0 || c.SugarCane > 0 || c.NetherWart > 0 || c.Seeds > 0)
			.OrderBy(c => c.Time)
			.FirstOrDefaultAsync();

		var lastCrops = await context.CropCollections
			.Where(c => c.ProfileMemberId == member.Id && c.Time >= startDate && c.Time <= endDate)
			.Where(c => c.Wheat > 0 || c.Carrot > 0 || c.Potato > 0 || c.Pumpkin > 0 || c.Melon > 0 || c.Mushroom > 0 ||
			            c.CocoaBeans > 0 || c.Cactus > 0 || c.SugarCane > 0 || c.NetherWart > 0 || c.Seeds > 0)
			.OrderByDescending(c => c.Time)
			.FirstOrDefaultAsync();

		var firstPests = await context.CropCollections
			.Where(c => c.ProfileMemberId == member.Id && c.Time >= startDate && c.Time <= endDate)
			.Where(c => c.Beetle > 0 || c.Cricket > 0 || c.Fly > 0 || c.Locust > 0 || c.Mite > 0 || c.Mosquito > 0 ||
			            c.Moth > 0 || c.Rat > 0 || c.Slug > 0 || c.Earthworm > 0 || c.Mouse > 0)
			.OrderBy(c => c.Time)
			.FirstOrDefaultAsync();

		var lastPests = await context.CropCollections
			.Where(c => c.ProfileMemberId == member.Id && c.Time >= startDate && c.Time <= endDate)
			.Where(c => c.Beetle > 0 || c.Cricket > 0 || c.Fly > 0 || c.Locust > 0 || c.Mite > 0 || c.Mosquito > 0 ||
			            c.Moth > 0 || c.Rat > 0 || c.Slug > 0 || c.Earthworm > 0 || c.Mouse > 0)
			.OrderByDescending(c => c.Time)
			.FirstOrDefaultAsync();

		var gainedCollection = new CropCollection();

		if (firstCrops != null && lastCrops != null) {
			foreach (var crop in CropColumns) {
				var firstVal = (long)typeof(CropCollection).GetProperty(crop)!.GetValue(firstCrops)!;
				var lastVal = (long)typeof(CropCollection).GetProperty(crop)!.GetValue(lastCrops)!;
				var increase = lastVal - firstVal;
				data.Collections.Increases[crop] = increase;
				
				typeof(CropCollection).GetProperty(crop)!.SetValue(gainedCollection, increase);
				
				if (globalStats.Crops.TryGetValue(crop, out var globalMedian)) {
					data.Collections.GlobalTotals[crop] = globalMedian;
					data.Collections.AverageComparison[crop] =
						Sanitize(globalMedian > 0 ? increase / (double)globalMedian : 0);
				}
			}
		}

		if (firstPests != null && lastPests != null) {
			var totalKills = 0;
			foreach (var pest in PestColumns) {
				var firstVal = (int)typeof(CropCollection).GetProperty(pest)!.GetValue(firstPests)!;
				var lastVal = (int)typeof(CropCollection).GetProperty(pest)!.GetValue(lastPests)!;
				var diff = lastVal - firstVal;
				data.Pests.Breakdown[pest] = diff;
				totalKills += diff;
				
				typeof(CropCollection).GetProperty(pest)!.SetValue(gainedCollection, diff);
			}

			data.Pests.Kills = totalKills;

			var globalPestTotal = globalStats.Pests.Values.Sum();

			data.Pests.GlobalTotal = globalPestTotal;
			data.Pests.AverageComparison = Sanitize(globalPestTotal > 0 ? totalKills / (double)globalPestTotal : 0);
		}

		// Calculate farming weight
		data.Player.FarmingWeight.Gained = Sanitize(gainedCollection.CountCropWeight());

		// Skills
		var firstSkills = await context.SkillExperiences
			.Where(s => s.ProfileMemberId == member.Id && s.Time >= startDate && s.Time <= endDate)
			.Where(s => (s.Farming + s.Mining + s.Combat + s.Foraging + s.Fishing + s.Enchanting + s.Alchemy +
			             s.Carpentry + s.Runecrafting + s.Social + s.Taming) > 0)
			.OrderBy(s => s.Time)
			.FirstOrDefaultAsync();

		var lastSkills = await context.SkillExperiences
			.Where(s => s.ProfileMemberId == member.Id && s.Time >= startDate && s.Time <= endDate)
			.Where(s => (s.Farming + s.Mining + s.Combat + s.Foraging + s.Fishing + s.Enchanting + s.Alchemy +
			             s.Carpentry + s.Runecrafting + s.Social + s.Taming) > 0)
			.OrderByDescending(s => s.Time)
			.FirstOrDefaultAsync();

		if (firstSkills != null && lastSkills != null) {
			double totalXpGained = 0;
			foreach (var skill in SkillColumns) {
				var firstVal = (double)typeof(SkillExperience).GetProperty(skill)!.GetValue(firstSkills)!;
				var lastVal = (double)typeof(SkillExperience).GetProperty(skill)!.GetValue(lastSkills)!;
				var diff = lastVal - firstVal;
				data.Skills.Breakdown[skill] = diff;
				totalXpGained += diff;
			}

			data.Skills.FarmingXp = data.Skills.Breakdown.GetValueOrDefault("Farming", 0);

			data.Skills.GlobalTotal = globalStats.TotalXp;
			if (globalStats.TotalXp > 0) {
				data.Skills.AverageComparison = Sanitize(totalXpGained / globalStats.TotalXp);
			}
		}

		// Leaderboards (Top 1000)
		var ranks = await lbService.GetPlayerLeaderboardEntriesWithRankAsync(member.Id);

		foreach (var rankData in ranks) {
			if (rankData.Rank is > 0 and <= 1000) {
				data.Leaderboards.Top1000.Add(new LeaderboardPlacementRecap {
					Title = rankData.Title,
					Slug = rankData.Slug,
					Rank = rankData.Rank,
					Amount = rankData.Amount,
					ShortTitle = rankData.Short
				});
			}
		}

		return data;
	}

	private static double Sanitize(double val) {
		if (double.IsInfinity(val) || double.IsNaN(val))
			return 0;
		return val;
	}

	private StreakRecap CalculateStreak(List<ContestParticipation> contests,
		List<(DateTimeOffset Time, long TotalCrops)> collections) {
		var points = new List<(DateTimeOffset Time, long Amount)>();

		// Add contest participations
		points.AddRange(contests.Select(c =>
			(DateTimeOffset.FromUnixTimeSeconds(c.JacobContest.Timestamp), (long)c.Collected)));

		// Add crop collections (only if increased)
		for (int i = 1; i < collections.Count; i++) {
			var diff = collections[i].TotalCrops - collections[i - 1].TotalCrops;
			if (diff > 0) {
				points.Add((collections[i].Time, diff));
			}
		}

		if (points.Count == 0)
			return new StreakRecap();

		points = points.OrderBy(p => p.Time).ToList();

		// Identify unique timestamps for streak calculation
		var uniqueTimes = points.Select(p => p.Time).Distinct().ToList();

		var bestIndex = 0;
		var bestCount = 0;
		var bestDuration = TimeSpan.Zero;
		var currentIndex = 0;

		for (var i = 1; i < uniqueTimes.Count; i++) {
			if (!((uniqueTimes[i] - uniqueTimes[i - 1]).TotalHours > 10)) continue;
			CheckStreak(uniqueTimes, currentIndex, i - 1, ref bestIndex, ref bestCount, ref bestDuration);
			currentIndex = i;
		}

		CheckStreak(uniqueTimes, currentIndex, uniqueTimes.Count - 1, ref bestIndex, ref bestCount, ref bestDuration);

		if (bestDuration == TimeSpan.Zero)
			return new StreakRecap();

		var streakTimes = uniqueTimes.Skip(bestIndex).Take(bestCount).ToList();
		var start = streakTimes.First();
		var end = streakTimes.Last();
		var totalDuration = end - start;

		// Sparkline Generation
		var totalHours = (int)Math.Ceiling(totalDuration.TotalHours);
		if (totalHours == 0) totalHours = 1;

		var sparkline = new long[totalHours + 1];

		// Filter points that fall within the streak range
		var streakPoints = points.Where(p => p.Time >= start && p.Time <= end).ToList();

		foreach (var p in streakPoints) {
			var offset = (int)(p.Time - start).TotalHours;
			if (offset >= 0 && offset < sparkline.Length) {
				sparkline[offset] += p.Amount;
			}
		}

		// Calculate downtime
		var inactiveHours = sparkline.Count(x => x == 0);
		double avgDowntime = 0;

		if (sparkline.Length > 0) {
			avgDowntime = ((double)inactiveHours / sparkline.Length) * 24.0;
		}

		return new StreakRecap {
			LongestStreakHours = (int)totalDuration.TotalHours,
			Start = start.ToUnixTimeSeconds(),
			End = end.ToUnixTimeSeconds(),
			AverageDailyDowntime = Math.Round(avgDowntime, 1),
			Sparkline = sparkline.ToList()
		};
	}

	private static void CheckStreak(List<DateTimeOffset> points, int start, int end, ref int bestIdx, ref int bestCount,
		ref TimeSpan bestDur) {
		if (end < start) return;

		var duration = points[end] - points[start];
		if (duration <= bestDur) return;
		
		bestDur = duration;
		bestIdx = start;
		bestCount = end - start + 1;
	}

	public async Task<bool> TogglePublicStatusAsync(string playerUuid, string profileUuid, int year, bool isPublic) {
		var profileMemberId = await context.ProfileMembers
			.Include(p => p.MinecraftAccount)
			.Where(pm => pm.PlayerUuid == playerUuid && pm.ProfileId == profileUuid)
			.Select(pm => pm.Id)
			.FirstOrDefaultAsync();

		if (profileMemberId == Guid.Empty) {
			return false;
		}

		var recap = await context.YearlyRecaps
			.FirstOrDefaultAsync(r => r.ProfileMemberId == profileMemberId && r.Year == year);

		if (recap == null) {
			return false;
		}

		recap.Public = isPublic;
		await context.SaveChangesAsync();

		return true;
	}

	public async Task<bool> IsPublicRecapAsync(string playerUuid, string profileUuid, int year) {
		var memberId = await memberService.GetProfileMemberId(playerUuid, profileUuid);
		if (memberId is null || memberId == Guid.Empty) {
			return false;
		}

		return await context.YearlyRecaps
			.Include(r => r.ProfileMember)
			.AnyAsync(r => r.ProfileMemberId == memberId && r.Year == year && r.Public);
	}

	/// <summary>
	/// Check if a year is valid for recap generation, must be 2025 or later, as long as the current year is past Dec 7th
	/// </summary>
	/// <param name="year"></param>
	/// <returns></returns>
	/// <exception cref="NotImplementedException"></exception>
	public bool ValidYear(int year) {
		var currentDate = DateTime.UtcNow;
		var validYear = currentDate.Year;

		if (currentDate.Month > 12 || currentDate is { Month: 12, Day: >= 7 }) {
			validYear += 1;
		}

		return year >= 2025 && year < validYear;
	}
}