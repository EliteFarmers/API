using EliteAPI.Features.Leaderboards.Services;
using EliteAPI.Models.Common;
using EliteAPI.Utilities;
using FastEndpoints;
using FastEndpoints.Swagger;

namespace EliteAPI.Features.Leaderboards.Endpoints.GetPlayerLeaderboardRanks;

internal sealed class LeaderboardRanksRequest : PlayerProfileUuidRequest
{
	/// <summary>
	/// Maximum rank number to return. Used if you don't want ranks higher than a certain number.
	/// </summary>
	[QueryParam]
	public int? Max { get; set; }
}

internal sealed class LeaderboardRanksResponse
{
	public Dictionary<string, PlayerLeaderboardEntryWithRankDto> Ranks { get; set; } = new();
}

internal sealed class GetPlayerLeaderboardRanksEndpoint(
	IRedisLeaderboardService redisLbService
) : Endpoint<LeaderboardRanksRequest, LeaderboardRanksResponse>
{
	public override void Configure() {
		Get("/leaderboards/{PlayerUuid}/{ProfileUuid}");
		AllowAnonymous();
		Version(0);

		Description(d => d.AutoTagOverride("Leaderboard"));
		Summary(s => { s.Summary = "Get a Player's Leaderboard Ranks"; });
	}

	public override async Task HandleAsync(LeaderboardRanksRequest request, CancellationToken c) {
		if (HttpContext.IsRequestDisabled()) {
			await Send.OkAsync(new LeaderboardRanksResponse(), c);
			return;
		}

		var response = new LeaderboardRanksResponse {
			Ranks = await redisLbService.GetCachedPlayerLeaderboardRanks(
				request.PlayerUuidFormatted,
				request.ProfileUuidFormatted,
				request.Max)
		};

		await Send.OkAsync(response, c);
	}
}
