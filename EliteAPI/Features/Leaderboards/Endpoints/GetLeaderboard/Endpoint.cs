using EliteAPI.Features.Leaderboards.Services;
using EliteAPI.Models.DTOs.Outgoing;
using FastEndpoints;

namespace EliteAPI.Features.Leaderboards.Endpoints.GetLeaderboard;

internal sealed class GetLeaderboardEndpoint(
	ILeaderboardService lbService
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
		if (!lbService.TryGetLeaderboardSettings(request.Leaderboard, out var lb)) {
			return; // This should be unreachable because the request is validated
		}
		var entries = await lbService.GetLeaderboardSlice(request.Leaderboard, request.OffsetFormatted, request.LimitFormatted);

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