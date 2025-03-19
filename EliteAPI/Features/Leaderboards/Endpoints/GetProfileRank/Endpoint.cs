using EliteAPI.Features.Leaderboards.Services;
using EliteAPI.Models.DTOs.Outgoing;
using FastEndpoints;

namespace EliteAPI.Features.Leaderboards.Endpoints.GetProfileRank;

internal sealed class GetProfileRankEndpoint(
	ILeaderboardService lbService
	) : Endpoint<GetProfileRankRequest, LeaderboardPositionDto>
{
	public override void Configure() {
		Get("/leaderboard/rank/{Leaderboard}/{ProfileUuid}");
		AllowAnonymous();
		Version(0);
		
		Description(s => s.Accepts<GetProfileRankRequest>());

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