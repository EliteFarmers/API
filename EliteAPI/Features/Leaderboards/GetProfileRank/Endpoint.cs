using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Services.Interfaces;
using FastEndpoints;

namespace EliteAPI.Features.Leaderboards.GetProfileRank;

internal sealed class GetProfileRankEndpoint(
	ILeaderboardService lbService
	) : Endpoint<GetProfileRankRequest, LeaderboardPositionDto>
{
	public override void Configure() {
		Get("/leaderboard/rank/{ProfileUuid}");
		AllowAnonymous();
		Version(0);

		Summary(s => {
			s.Summary = "Get a Profiles's Leaderboard Rank";
		});
	}

	public override async Task HandleAsync(GetProfileRankRequest request, CancellationToken c) {
		var result = await lbService.GetLeaderboardRank(
			leaderboardId: request.Leaderboard,
			playerUuid: string.Empty,
			profileId: request.ProfileUuidFormatted,
			includeUpcoming: request.IncludeUpcoming ?? false,
			atRank: request.AtRank ?? -1,
			c: c
		);

		if (result is null) {
			ThrowError("Profile not found", StatusCodes.Status404NotFound);
		}
		
		await SendAsync(result, cancellation: c);
	}
}