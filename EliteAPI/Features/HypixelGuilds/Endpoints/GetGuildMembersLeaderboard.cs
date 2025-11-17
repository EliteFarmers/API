using EliteAPI.Features.Leaderboards;
using EliteAPI.Features.Leaderboards.Endpoints;
using EliteAPI.Features.Leaderboards.Services;
using EliteAPI.Models.DTOs.Outgoing;
using FastEndpoints;
using FastEndpoints.Swagger;

namespace EliteAPI.Features.HypixelGuilds.Endpoints;

public class GetHypixelGuildMembersLeaderboardRequest : LeaderboardRequest
{
	/// <summary>
	/// Guild id to fetch members for (discord/hypixel guild id)
	/// </summary>
	public required string GuildId { get; set; }
}

public class GuildMembersLeaderboardDto : LeaderboardDto
{
	public required string GuildId { get; set; }
}

internal sealed class GetHypixelGuildMembersLeaderboardEndpoint(
	ILbService lbService,
	ILeaderboardRegistrationService leaderboardRegistrationService
) : Endpoint<GetHypixelGuildMembersLeaderboardRequest, GuildMembersLeaderboardDto>
{
	public override void Configure() {
		Get("/hguilds/{GuildId}/leaderboards/{Leaderboard}");
		AllowAnonymous();
		Version(0);

		Description(d => d.AutoTagOverride("Hypixel Guilds"));
		Summary(s => { s.Summary = "Get Hypixel Guild Members Leaderboard"; });
	}

	public override async Task HandleAsync(GetHypixelGuildMembersLeaderboardRequest request, CancellationToken c) {
		if (!leaderboardRegistrationService.LeaderboardsById.TryGetValue(request.Leaderboard, out var lb))
			ThrowError("Leaderboard does not exist", StatusCodes.Status404NotFound);
		
		var entries = await lbService.GetGuildMembersLeaderboardEntriesAsync(
			request.GuildId,
			request.Leaderboard,
			request.Interval,
			request.Mode
		);
		
		var type = LbService.GetTypeFromSlug(request.Leaderboard);
		var time = request.Interval is not null
			? lbService.GetIntervalTimeRange(request.Interval)
			: lbService.GetCurrentTimeRange(type);

		var firstInterval = await lbService.GetFirstInterval(request.Leaderboard);

		var leaderboard = new GuildMembersLeaderboardDto {
			GuildId = request.GuildId,
			Id = request.Leaderboard,
			Title = lb.Info.Title,
			ShortTitle = lb.Info.ShortTitle,
			Interval = request.Interval ?? LbService.GetCurrentIdentifier(type),
			FirstInterval = firstInterval,
			Limit = entries.Count,
			Offset = 0,
			MinimumScore = lb.Info.MinimumScore,
			StartsAt = time.start,
			EndsAt = time.end,
			MaxEntries = -1,
			Profile = lb is IProfileLeaderboardDefinition,
			Entries = entries
		};

		await Send.OkAsync(leaderboard, c);
	}
}

