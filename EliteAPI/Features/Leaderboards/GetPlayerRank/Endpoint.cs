using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Services.Interfaces;
using FastEndpoints;

namespace EliteAPI.Features.Leaderboards.GetPlayerRank;

internal sealed class GetPlayerRankEndpoint(
	ILeaderboardService lbService
	) : Endpoint<GetPlayerRankRequest, LeaderboardPositionDto>
{
	
	public override void Configure() {
		Get("/leaderboard/rank/{Leaderboard}/{PlayerUuid}/{ProfileUuid}");
		AllowAnonymous();
		Version(0);

		Summary(s => {
			s.Summary = "Get a Player's Leaderboard Rank";
		});
	}

	public override async Task HandleAsync(GetPlayerRankRequest request, CancellationToken c) {
		var result = await lbService.GetLeaderboardRank(
			leaderboardId: request.Leaderboard,
			playerUuid: request.PlayerUuidFormatted,
			profileId: request.ProfileUuidFormatted,
			includeUpcoming: request.IncludeUpcoming ?? false,
			atRank: request.AtRank ?? -1,
			c: c
		);

		if (result is null) {
			ThrowError("Player not found", StatusCodes.Status404NotFound);
		}
		
		await SendAsync(result, cancellation: c);
	}
}