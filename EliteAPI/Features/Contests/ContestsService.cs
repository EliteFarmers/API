using System.Text.Json;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Parsers.Farming;
using EliteAPI.Utilities;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using IMapper = AutoMapper.IMapper;

namespace EliteAPI.Features.Contests;

public interface IContestsService
{
	Task<List<ContestParticipationWithTimestampDto>> FetchRecords(Crop crop, long startTime, long endTime);
	Task<List<JacobContestWithParticipationsDto>> GetContestsAt(long timestamp, int limit = -1);
	Task<JacobContestWithParticipationsDto?> GetContestFromKey(string contestKey);
	Task<YearlyContestsDto> GetContestsFromYear(int year, bool now = false);
	Task<Dictionary<string, ContestBracketsDto>?> GetAverageMedalBrackets(long start, long end);
}

[RegisterService<IContestsService>(LifeTime.Scoped)]
public class ContestsService(
	DataContext context,
	ILogger<ContestsService> logger,
	IMapper mapper,
	IConnectionMultiplexer redis)
	: IContestsService
{
	public async Task<List<JacobContestWithParticipationsDto>> GetContestsAt(long timestamp, int limit = -1) {
		var skyblockDate = new SkyblockDate(timestamp);
		if (skyblockDate.Year < 1) return [];

		var contests = await context.JacobContests
			.Where(j => j.Timestamp == timestamp)
			.ToListAsync();

		if (contests.Count == 0) return [];

		var data = mapper.Map<List<JacobContestWithParticipationsDto>>(contests);

		foreach (var contest in contests) {
			var participations = await context.ContestParticipations
				.Where(p => p.JacobContestId == contest.Id)
				.Include(p => p.ProfileMember.MinecraftAccount)
				.ToListAsync();

			var crop = FormatUtils.GetFormattedCropName(contest.Crop);

			var stripped = mapper.Map<List<StrippedContestParticipationDto>>(participations);

			var contestDto = data.First(d => d.Crop.Equals(crop));

			contestDto.Participations = stripped
				.OrderByDescending(p => p.Collected)
				.Take(limit > 0 ? limit : stripped.Count)
				.ToList();
		}

		return data;
	}

	public async Task<List<ContestParticipationWithTimestampDto>>
		FetchRecords(Crop crop, long startTime, long endTime) {
		var cropInt = (int)crop;

		try {
			// Work around EF Core not supporting DISTINCT ON
			// Also work around EF not supporting mapping to a DTO by parsing as JSON
			var asJson = await context.Database.SqlQuery<string>($"""
			                                                      	SELECT json_agg(c) as "Value"
			                                                      	FROM (
			                                                      	SELECT 
			                                                      	    "Collected", "Position", "Crop", 
			                                                      	    "Timestamp", "Participants", "PlayerUuid", 
			                                                      	    "ProfileId" as "ProfileUuid", 
			                                                      	    "Name" as "PlayerName",
			                                                      	    "WasRemoved" as "Removed"
			                                                      	FROM (
			                                                      	    SELECT 
			                                                      	        DISTINCT ON ("ProfileMemberId") "ProfileMemberId", 
			                                                      	        "Collected", "Position", "Crop", "Timestamp", "Participants"
			                                                      	    FROM "ContestParticipations"
			                                                      	    LEFT JOIN "JacobContests" ON "JacobContestId" = "JacobContests"."Id"
			                                                      	    WHERE "Crop" = {cropInt} AND "JacobContestId" BETWEEN {startTime} AND {endTime}
			                                                      	    ORDER BY "ProfileMemberId", "Collected" DESC
			                                                      	) sub
			                                                      	LEFT JOIN "ProfileMembers" ON sub."ProfileMemberId" = "ProfileMembers"."Id"
			                                                      	LEFT JOIN "MinecraftAccounts" ON "PlayerUuid" = "MinecraftAccounts"."Id"
			                                                      	ORDER BY "Collected" DESC
			                                                      	LIMIT 100
			                                                      	) c
			                                                      """).FirstOrDefaultAsync();

			if (asJson is null) return [];
			var parsed = JsonSerializer.Deserialize<List<ContestParticipationWithTimestampDto>>(asJson);

			return parsed ?? [];
		}
		catch (Exception e) {
			logger.LogError(e, "Failed to deserialize contest records");
			return [];
		}
	}

	public async Task<JacobContestWithParticipationsDto?> GetContestFromKey(string contestKey) {
		var timestamp = FormatUtils.GetTimeFromContestKey(contestKey);
		var cropId = FormatUtils.GetCropFromContestKey(contestKey);

		if (timestamp == 0 || cropId is null) return null;

		var contest = await context.JacobContests
			.Where(j => j.Timestamp == timestamp && j.Crop == cropId)
			.FirstOrDefaultAsync();

		if (contest is null) return null;

		var data = mapper.Map<JacobContestWithParticipationsDto>(contest);

		var participations = await context.ContestParticipations
			.Where(p => p.JacobContestId == contest.Id)
			.Include(p => p.ProfileMember.MinecraftAccount)
			.ToListAsync();

		var stripped = mapper.Map<List<StrippedContestParticipationDto>>(participations);

		data.Participations = stripped.OrderByDescending(p => p.Collected).ToList();
		return data;
	}

	public async Task<YearlyContestsDto> GetContestsFromYear(int year, bool now = false) {
		var currentDate = SkyblockDate.Now;

		if (currentDate.Year == year - 1) {
			var db = redis.GetDatabase();

			var data = await db.StringGetAsync($"contests:{currentDate.Year}");
			if (data.HasValue)
				try {
					var sourcedContests = JsonSerializer.Deserialize<Dictionary<long, List<string>>>(data.ToString());

					return new YearlyContestsDto {
						Year = currentDate.Year + 1,
						Count = (sourcedContests?.Count ?? 0) * 3,
						Complete = sourcedContests?.Count == 124,
						Contests = sourcedContests ?? new Dictionary<long, List<string>>()
					};
				}
				catch (Exception e) {
					logger.LogError(e, "Failed to deserialize cached contests data");
				}
		}

		var startTime = FormatUtils.GetTimeFromSkyblockDate(year - 1, 0, 0);
		var endTime = FormatUtils.GetTimeFromSkyblockDate(year, 0, 0);

		var contests = await context.JacobContests
			.Where(j => j.Timestamp > startTime && j.Timestamp < endTime)
			.ToListAsync();

		var result = new Dictionary<long, List<string>>();
		foreach (var contest in contests) {
			if (!result.TryGetValue(contest.Timestamp, out var value)) {
				value = [];
				result.Add(contest.Timestamp, value);
			}

			var crop = FormatUtils.GetFormattedCropName(contest.Crop);

			value.Add(crop);
		}

		return new YearlyContestsDto {
			Year = year,
			Count = contests.Count,
			Complete = contests.Count == 372,
			Contests = result
		};
	}

	private static readonly JsonSerializerOptions Options = new() {
		PropertyNameCaseInsensitive = true
	};

	public async Task<Dictionary<string, ContestBracketsDto>?> GetAverageMedalBrackets(long start, long end) {
		var medals = await context.Database
			.SqlQuery<string>($"""
			                   SELECT json_agg(c) AS "Value"
			                   FROM (
			                      SELECT
			                          "Crop",
			                          AVG(NULLIF("Diamond", 0)) AS "Diamond",
			                          AVG(NULLIF("Platinum", 0)) AS "Platinum",
			                          AVG(NULLIF("Gold", 0)) AS "Gold",
			                          AVG(NULLIF("Silver", 0)) AS "Silver",
			                          AVG(NULLIF("Bronze", 0)) AS "Bronze"
			                      FROM "JacobContests"
			                      WHERE "Timestamp" >= {start} AND "Timestamp" <= {end}
			                      GROUP BY "Crop"
			                   ) c
			                   """)
			.ToListAsync();

		try {
			var parsed = JsonSerializer.Deserialize<List<MedalCutoffsDbDto>>(medals.First(), Options);

			var dto = parsed?.ToDictionary(
				m => ((Crop)m.Crop).SimpleName(),
				m => new ContestBracketsDto {
					Diamond = (int)(m.Diamond ?? 0),
					Platinum = (int)(m.Platinum ?? 0),
					Gold = (int)(m.Gold ?? 0),
					Silver = (int)(m.Silver ?? 0),
					Bronze = (int)(m.Bronze ?? 0)
				}) ?? new Dictionary<string, ContestBracketsDto>();

			// Add missing crops to the dictionary
			if (dto.Count < Enum.GetValues<Crop>().Length - 1) {
				foreach (var crop in Enum.GetValues<Crop>()) {
					var name = crop.SimpleName();
					if (name == "seeds") continue;
					dto.TryAdd(name, new ContestBracketsDto());
				}
			}

			return dto;
		}
		catch {
			return null;
		}
	}
}