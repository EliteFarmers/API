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

			await Send.OkAsync(result, cancellation: c);
			return;
		}
		
		var newResult = await newLbService.GetLeaderboardRank(
			leaderboardId: request.Leaderboard,
			playerUuid: string.Empty,
			profileId: request.ProfileUuidFormatted,
			upcoming: request.Upcoming,
			atRank: request.AtRank ?? -1,
			identifier: request.Interval,
			gameMode: request.Mode,
			removedFilter: request.Removed ?? RemovedFilter.NotRemoved,
			c: c
		);
		
		if (newResult is null) {
			var last = await newLbService.GetLastLeaderboardEntry(
				request.Leaderboard,
				removedFilter: request.Removed ?? RemovedFilter.NotRemoved,
				gameMode: request.Mode,
				identifier: request.Interval);
			await Send.OkAsync(new LeaderboardPositionDto {
				Rank = -1,
				Amount = 0,
				MinAmount = newLbService.GetLeaderboardMinScore(request.Leaderboard),
				UpcomingRank = last?.Rank ?? 10_000,
				UpcomingPlayers = request.Upcoming > 0 && last is not null ? [last] : null,
			}, cancellation: c);
			return;
		}
		
		await Send.OkAsync(newResult, cancellation: c);
	}
}