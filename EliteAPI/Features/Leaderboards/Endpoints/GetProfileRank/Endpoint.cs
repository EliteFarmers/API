using EliteAPI.Features.Leaderboards.Services;
using EliteAPI.Models.DTOs.Outgoing;
using FastEndpoints;

namespace EliteAPI.Features.Leaderboards.Endpoints.GetProfileRank;

internal sealed class GetProfileRankEndpoint(
	ILbService lbService
) : Endpoint<GetProfileRankRequest, LeaderboardPositionDto>
{
	public override void Configure() {
		Get("/leaderboard/rank/{Leaderboard}/{ProfileUuid}", "/leaderboard/{Leaderboard}/{ProfileUuid}");
		AllowAnonymous();
		Version(0);

		Description(s => s.Accepts<GetProfileRankRequest>());

		Summary(s => { s.Summary = "Get a Profiles's Leaderboard Rank"; });
	}

	public override async Task HandleAsync(GetProfileRankRequest request, CancellationToken c) {
#pragma warning disable CS0618 // Type or member is obsolete
		if (request is { IncludeUpcoming: true, Upcoming: 0 or null }) request.Upcoming = 5;
#pragma warning restore CS0618 // Type or member is obsolete

		var result = await lbService.GetLeaderboardRank(
			request.Leaderboard,
			string.Empty,
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