using EliteAPI.Features.Leaderboards.Services;
using EliteAPI.Models.DTOs.Outgoing;
using FastEndpoints;

namespace EliteAPI.Features.Leaderboards.Endpoints.GetLeaderboard;

internal sealed class GetLeaderboardEndpoint(
	ILbService lbService,
	ILeaderboardRegistrationService leaderboardRegistrationService
) : Endpoint<LeaderboardSliceRequest, LeaderboardDto>
{
	public override void Configure() {
		Get("/leaderboard/{Leaderboard}");
		AllowAnonymous();
		Version(0);

		Description(s => s.Accepts<LeaderboardSliceRequest>());

		Summary(s => { s.Summary = "Get Leaderboard"; });
	}

	public override async Task HandleAsync(LeaderboardSliceRequest request, CancellationToken c) {
		if (!leaderboardRegistrationService.LeaderboardsById.TryGetValue(request.Leaderboard, out var newLb))
			ThrowError("Leaderboard does not exist", StatusCodes.Status404NotFound);

		var newEntries = await lbService.GetLeaderboardSlice(
			request.Leaderboard,
			request.OffsetFormatted,
			request.LimitFormatted,
			removedFilter: request.Removed ?? RemovedFilter.NotRemoved,
			gameMode: request.Mode,
			identifier: request.Interval);

		var type = LbService.GetTypeFromSlug(request.Leaderboard);
		var time = request.Interval is not null
			? lbService.GetIntervalTimeRange(request.Interval)
			: lbService.GetCurrentTimeRange(type);

		var lastEntry = await lbService.GetLastLeaderboardEntry(
			request.Leaderboard,
			removedFilter: request.Removed ?? RemovedFilter.NotRemoved,
			gameMode: request.Mode,
			identifier: request.Interval);

		var firstInterval = await lbService.GetFirstInterval(request.Leaderboard);

		var newLeaderboard = new LeaderboardDto {
			Id = request.Leaderboard,
			Title = newLb.Info.Title,
			ShortTitle = newLb.Info.ShortTitle,
			Interval = request.Interval ?? LbService.GetCurrentIdentifier(type),
			FirstInterval = firstInterval,
			Limit = request.LimitFormatted,
			Offset = request.OffsetFormatted,
			MinimumScore = newLb.Info.MinimumScore,
			StartsAt = time.start,
			EndsAt = time.end,
			MaxEntries = lastEntry?.Rank ?? -1,
			Profile = newLb is IProfileLeaderboardDefinition,
			Entries = newEntries
		};

		await Send.OkAsync(newLeaderboard, c);
	}
}