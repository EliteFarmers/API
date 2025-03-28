using EliteAPI.Features.Leaderboards.Services;
using EliteAPI.Models.DTOs.Outgoing;
using FastEndpoints;

namespace EliteAPI.Features.Leaderboards.Endpoints.GetPlayerRank;

internal sealed class GetPlayerRankEndpoint(
	ILeaderboardService lbService,
	ILbService newLbService
	) : Endpoint<GetPlayerRankRequest, LeaderboardPositionDto>
{
	
	public override void Configure() {
		Get("/leaderboard/rank/{Leaderboard}/{PlayerUuid}/{ProfileUuid}", "/leaderboard/{Leaderboard}/{PlayerUuid}/{ProfileUuid}");
		AllowAnonymous();
		Version(0);
		
		Description(s => s.Accepts<GetPlayerRankRequest>());

		Summary(s => {
			s.Summary = "Get a Player's Leaderboard Rank";
		});
	}

	public override async Task HandleAsync(GetPlayerRankRequest request, CancellationToken c) {
#pragma warning disable CS0618 // Type or member is obsolete
		if (request is { IncludeUpcoming: true, Upcoming: 0 or null }) {
			request.Upcoming = 10;
		}
#pragma warning restore CS0618 // Type or member is obsolete

		if (request.New is not true) {
			var result = await lbService.GetLeaderboardRank(
				leaderboardId: request.Leaderboard,
				playerUuid: request.PlayerUuidFormatted,
				profileId: request.ProfileUuidFormatted,
				includeUpcoming: request.Upcoming > 0,
				atRank: request.AtRank ?? -1,
				c: c
			);

			if (result is null) {
				ThrowError("Player not found", StatusCodes.Status404NotFound);
			}

			await SendAsync(result, cancellation: c);
			return;
		}
		
		var newResult = await newLbService.GetLeaderboardRank(
			leaderboardId: request.Leaderboard,
			playerUuid: request.PlayerUuidFormatted,
			profileId: request.ProfileUuidFormatted,
			upcoming: request.Upcoming,
			atRank: request.AtRank ?? -1,
			c: c
		);
		
		if (newResult is null) {
			var last = await newLbService.GetLastLeaderboardEntry(request.Leaderboard);
			await SendAsync(new LeaderboardPositionDto {
				Rank = -1,
				Amount = 0,
				UpcomingRank = last?.Rank ?? 10_000,
				UpcomingPlayers = request.Upcoming > 0 && last is not null ? [last] : null,
			}, cancellation: c);
			return;
		}
		
		await SendAsync(newResult, cancellation: c);
	}
}