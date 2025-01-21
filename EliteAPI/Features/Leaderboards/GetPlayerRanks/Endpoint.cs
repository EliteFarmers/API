using EliteAPI.Data;
using EliteAPI.Models.Common;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Services.Interfaces;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Leaderboards.GetPlayerRanks;

internal sealed class GetPlayerRanksEndpoint(
	DataContext dataContext,
	ILeaderboardService lbService
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
		var memberId = await dataContext.ProfileMembers.AsNoTracking()
			.Where(p => p.ProfileId.Equals(request.ProfileUuidFormatted) && p.PlayerUuid.Equals(request.PlayerUuidFormatted))
			.Select(p => p.Id)
			.FirstOrDefaultAsync(cancellationToken: c);

		if (memberId == Guid.Empty) {
			ThrowError("Profile member not found.", StatusCodes.Status404NotFound);
		}
        
		var positions = await lbService.GetLeaderboardPositions(memberId.ToString(), request.ProfileUuidFormatted);
		
		await SendAsync(positions, cancellation: c);
	}
}