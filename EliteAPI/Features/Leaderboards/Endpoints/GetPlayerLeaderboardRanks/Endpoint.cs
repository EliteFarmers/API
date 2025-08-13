using EliteAPI.Features.Leaderboards.Services;
using EliteAPI.Features.Profiles.Services;
using EliteAPI.Models.Common;
using FastEndpoints;
using FastEndpoints.Swagger;

namespace EliteAPI.Features.Leaderboards.Endpoints.GetPlayerLeaderboardRanks;

internal sealed class LeaderboardRanksRequest : PlayerProfileUuidRequest {
	/// <summary>
	/// Maximum rank number to return. Used if you don't want ranks higher than a certain number.
	/// </summary>
	[QueryParam]
	public int? Max { get; set; }
}

internal sealed class LeaderboardRanksResponse {
	public Dictionary<string, PlayerLeaderboardEntryWithRankDto> Ranks { get; set; } = new();
}

internal sealed class GetPlayerLeaderboardRanksEndpoint(
	ILbService lbService,
	IMemberService memberService
	) : Endpoint<LeaderboardRanksRequest, LeaderboardRanksResponse> 
{
	public override void Configure() {
		Get("/leaderboards/{PlayerUuid}/{ProfileUuid}");
		AllowAnonymous();
		Version(0);
		
		Description(d => d.AutoTagOverride("Leaderboard"));
		Summary(s => {
			s.Summary = "Get a Player's Leaderboard Ranks";
		});
	}

	public override async Task HandleAsync(LeaderboardRanksRequest request, CancellationToken c) {
		var member = await memberService.GetProfileMemberId(request.PlayerUuidFormatted, request.ProfileUuidFormatted);
		if (member is null) {
			ThrowError("Profile member not found.", StatusCodes.Status404NotFound);
		}
		
		var entries = await lbService.GetPlayerLeaderboardEntriesWithRankAsync(member.Value);
		var profileEntries = await lbService.GetProfileLeaderboardEntriesWithRankAsync(request.ProfileUuidFormatted);
		
		var ranks = entries.Concat(profileEntries)
			.Where(e => request.Max is null || e.Rank <= request.Max)
			.ToDictionary(e => e.Slug, e => e);

		var response = new LeaderboardRanksResponse {
			Ranks = ranks
		};
		
		await Send.OkAsync(response, cancellation: c);
	}
}