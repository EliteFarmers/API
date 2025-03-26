using EliteAPI.Features.Leaderboards.Services;
using EliteAPI.Models.DTOs.Outgoing;
using FastEndpoints;

namespace EliteAPI.Features.Leaderboards.Endpoints.GetProfileRank;

internal sealed class GetProfileRankEndpoint(
	ILeaderboardService lbService,
	ILbService newLbService
	) : Endpoint<GetProfileRankRequest, LeaderboardPositionDto>
{
	public override void Configure() {
		Get("/leaderboard/rank/{Leaderboard}/{ProfileUuid}", "/leaderboard/{Leaderboard}/{ProfileUuid}");
		AllowAnonymous();
		Version(0);
		
		Description(s => s.Accepts<GetProfileRankRequest>());

		Summary(s => {
			s.Summary = "Get a Profiles's Leaderboard Rank";
		});
	}

	public override async Task HandleAsync(GetProfileRankRequest request, CancellationToken c) {
#pragma warning disable CS0618 // Type or member is obsolete
		if (request is { IncludeUpcoming: true, Upcoming: 0 or null }) {
			request.Upcoming = 5;
		}
#pragma warning restore CS0618 // Type or member is obsolete

		if (request.New is not true) {
			var result = await lbService.GetLeaderboardRank(
				leaderboardId: request.Leaderboard,
				playerUuid: string.Empty,
				profileId: request.ProfileUuidFormatted,
				includeUpcoming: request.Upcoming > 0,
				atRank: request.AtRank ?? -1,
				c: c
			);

			if (result is null) {
				ThrowError("Profile not found", StatusCodes.Status404NotFound);
			}

			await SendAsync(result, cancellation: c);
			return;
		}
		
		var newResult = await newLbService.GetLeaderboardRank(
			leaderboardId: request.Leaderboard,
			playerUuid: string.Empty,
			profileId: request.ProfileUuidFormatted,
			upcoming: request.Upcoming,
			atRank: request.AtRank ?? -1,
			c: c
		);
		
		if (newResult is null) {
			ThrowError("Profile not found", StatusCodes.Status404NotFound);
		}
		
		await SendAsync(newResult, cancellation: c);
	}
}