using EliteAPI.Features.Leaderboards.Services;
using EliteAPI.Models.Common;
using EliteAPI.Models.DTOs.Outgoing;
using FastEndpoints;

namespace EliteAPI.Features.Leaderboards.Endpoints.GetPlayerRanks;

[Obsolete]
internal sealed class GetPlayerRanksEndpoint() : Endpoint<PlayerProfileUuidRequest, LeaderboardPositionsDto> 
{
	public override void Configure() {
		Get("/leaderboard/ranks/{PlayerUuid}/{ProfileUuid}");
		AllowAnonymous();
		Version(0);
		
		Summary(s => {
			s.Summary = "Get a Player's Leaderboard Ranks";
		});
	}

	public override Task HandleAsync(PlayerProfileUuidRequest request, CancellationToken c) {
		ThrowError("This endpoint is deprecated and will be removed in a future version. Please use the new endpoints for leaderboard functionality.", StatusCodes.Status410Gone);
		return Task.CompletedTask;
	}
}