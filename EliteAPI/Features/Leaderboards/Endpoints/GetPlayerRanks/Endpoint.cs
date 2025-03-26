using EliteAPI.Features.Leaderboards.Services;
using EliteAPI.Models.Common;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Services.Interfaces;
using FastEndpoints;

namespace EliteAPI.Features.Leaderboards.Endpoints.GetPlayerRanks;

[Obsolete]
internal sealed class GetPlayerRanksEndpoint(
	ILeaderboardService lbService,
	IMemberService memberService
	) : Endpoint<PlayerProfileUuidRequest, LeaderboardPositionsDto> 
{
	public override void Configure() {
		Get("/leaderboard/ranks/{PlayerUuid}/{ProfileUuid}");
		AllowAnonymous();
		Version(0);
		
		Summary(s => {
			s.Summary = "Get a Player's Leaderboard Ranks";
		});
	}

	public override async Task HandleAsync(PlayerProfileUuidRequest request, CancellationToken c) {
		var memberId = await memberService.GetProfileMemberId(request.PlayerUuidFormatted, request.ProfileUuidFormatted);

		if (memberId is null) {
			ThrowError("Profile member not found.", StatusCodes.Status404NotFound);
		}
        
		var positions = await lbService.GetLeaderboardPositions(memberId.Value.ToString(), request.ProfileUuidFormatted);
		
		await SendAsync(positions, cancellation: c);
	}
}