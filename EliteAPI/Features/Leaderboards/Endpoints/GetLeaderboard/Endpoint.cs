using EliteAPI.Features.Leaderboards.Services;
using EliteAPI.Models.DTOs.Outgoing;
using FastEndpoints;

namespace EliteAPI.Features.Leaderboards.Endpoints.GetLeaderboard;

internal sealed class GetLeaderboardEndpoint(
	ILeaderboardService leaderboardService,
	ILbService newLbService,
	ILeaderboardRegistrationService leaderboardRegistrationService
	) : Endpoint<LeaderboardSliceRequest, LeaderboardDto> 
{
	public override void Configure() {
		Get("/leaderboard/{Leaderboard}");
		AllowAnonymous();
		Version(0);
		
		Description(s => s.Accepts<LeaderboardSliceRequest>());

		Summary(s => {
			s.Summary = "Get Leaderboard";
		});
	}

	public override async Task HandleAsync(LeaderboardSliceRequest request, CancellationToken c) {
		if (request.New is true && leaderboardRegistrationService.LeaderboardsById.TryGetValue(request.Leaderboard, out var newLb)) {
			var newEntries = await newLbService.GetLeaderboardSlice(request.Leaderboard, request.OffsetFormatted,
				request.LimitFormatted);

			var type = LbService.GetTypeFromSlug(request.Leaderboard);
			var time = newLbService.GetCurrentTimeRange(type);

			var newLeaderboard = new LeaderboardDto {
				Id = request.Leaderboard,
				Title = newLb.Info.Title,
				ShortTitle = newLb.Info.ShortTitle,
				Limit = request.LimitFormatted,
				Offset = request.OffsetFormatted,
				MinimumScore = newLb.Info.MinimumScore,
				StartsAt = time.start,
				EndsAt = time.end,
				MaxEntries = -1,
				Profile = newLb is IProfileLeaderboardDefinition,
				Entries = newEntries
			};

			await SendAsync(newLeaderboard, cancellation: c);
			return;
		}
		
		
		if (!leaderboardService.TryGetLeaderboardSettings(request.Leaderboard, out var lb)) {
			return; // This should be unreachable because the request is validated
		}

		var entries = await leaderboardService.GetLeaderboardSlice(request.Leaderboard, request.OffsetFormatted, request.LimitFormatted);

		var leaderboard = new LeaderboardDto {
			Id = request.Leaderboard,
			Title = lb.Title,
			Limit = request.LimitFormatted,
			Offset = request.OffsetFormatted,
			MaxEntries = lb.Limit,
			Profile = lb.Profile,
			Entries = entries.Select(e => e.MapToDto()).ToList()
		};

		await SendAsync(leaderboard, cancellation: c);
	}
}