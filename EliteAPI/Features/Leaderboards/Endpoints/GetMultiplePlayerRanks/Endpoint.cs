using EliteAPI.Features.Leaderboards.Models;
using EliteAPI.Features.Leaderboards.Services;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Utilities;
using FastEndpoints;
using FastEndpoints.Swagger;

namespace EliteAPI.Features.Leaderboards.Endpoints.GetMultiplePlayerRanks;

internal sealed class GetMultiplePlayerRanksEndpoint(
	ILbService lbService,
	IRedisLeaderboardService redisLbService
) : Endpoint<GetMultiplePlayerRanksRequest, Dictionary<string, LeaderboardPositionDto?>>
{
	public override void Configure() {
		Get("/leaderboards-multiple/{PlayerUuid}/{ProfileUuid}");
		AllowAnonymous();
		Version(0);

		Description(d => d.AutoTagOverride("Leaderboard"));

		Summary(s => { s.Summary = "Get multiple leaderboard ranks for a player"; });
	}

	public override async Task HandleAsync(GetMultiplePlayerRanksRequest request, CancellationToken c) {
#pragma warning disable CS0618 // Type or member is obsolete
		if (request is { IncludeUpcoming: true, Upcoming: 0 or null }) request.Upcoming = 10;
#pragma warning restore CS0618 // Type or member is obsolete

		if (HttpContext.IsRequestDisabled()) {
			await Send.OkAsync(new Dictionary<string, LeaderboardPositionDto?>(), c);
		}

		if (HttpContext.IsSkyHanni()) {
			var req = new LeaderboardRankRequestWithoutId() {
				PlayerUuid = request.PlayerUuidFormatted,
				ProfileId = request.ProfileUuidFormatted,
				Upcoming = request.Upcoming,
				Previous = request.Previous,
				AtRank = request.AtRank,
				Identifier = request.Interval,
				GameMode = request.Mode,
				RemovedFilter = request.Removed ?? RemovedFilter.NotRemoved,
				CancellationToken = c
			};
			
			await Send.OkAsync(await redisLbService.GetMultipleLeaderboardRanks(request.LeaderboardList, req), c);
			return;
		}

		var result = await lbService.GetMultipleLeaderboardRanks(
			request.LeaderboardList,
			request.PlayerUuidFormatted,
			request.ProfileUuidFormatted,
			request.Upcoming,
			request.Previous,
			request.AtRank ?? -1,
			identifier: request.Interval,
			gameMode: request.Mode,
			removedFilter: request.Removed ?? RemovedFilter.NotRemoved,
			c: c
		);

		await Send.OkAsync(result, c);
	}
}