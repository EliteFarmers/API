using EliteAPI.Features.Leaderboards.Services;
using EliteAPI.Models.DTOs.Outgoing;
using FastEndpoints;
using FastEndpoints.Swagger;

namespace EliteAPI.Features.Leaderboards.Endpoints.GetMultiplePlayerRanks;

internal sealed class GetMultiplePlayerRanksEndpoint(
	ILbService lbService
) : Endpoint<GetMultiplePlayerRanksRequest, Dictionary<string, LeaderboardPositionDto?>> {
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