using EliteAPI.Features.Leaderboards.Services;
using EliteAPI.Models.DTOs.Outgoing;
using FastEndpoints;

namespace EliteAPI.Features.Leaderboards.Endpoints.GetPlayerRank;

internal sealed class GetPlayerRankEndpoint(
	ILbService lbService
) : Endpoint<GetPlayerRankRequest, LeaderboardPositionDto> {
	public override void Configure() {
		Get("/leaderboard/rank/{Leaderboard}/{PlayerUuid}/{ProfileUuid}",
			"/leaderboard/{Leaderboard}/{PlayerUuid}/{ProfileUuid}");
		AllowAnonymous();
		Version(0);

		Summary(s => { s.Summary = "Get a Player's Leaderboard Rank"; });
	}

	public override async Task HandleAsync(GetPlayerRankRequest request, CancellationToken c) {
#pragma warning disable CS0618 // Type or member is obsolete
		if (request is { IncludeUpcoming: true, Upcoming: 0 or null }) request.Upcoming = 10;
#pragma warning restore CS0618 // Type or member is obsolete

		var result = await lbService.GetLeaderboardRank(
			request.Leaderboard,
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