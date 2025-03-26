using EliteAPI.Features.Leaderboards.Models;
using EliteAPI.Features.Leaderboards.Services;
using EliteAPI.Utilities;
using FastEndpoints;
using FastEndpoints.Swagger;

namespace EliteAPI.Features.Leaderboards.Endpoints.GetLeaderboards;

internal sealed class LeaderboardsResponse {
	public required Dictionary<string, LeaderboardInfoDto> Leaderboards { get; set; }
}

internal sealed class GetLeaderboardsEndpoint(
	ILeaderboardRegistrationService lbRegistrationService)
	: EndpointWithoutRequest<LeaderboardsResponse> 
{
	public override void Configure() {
		Get("/leaderboards");
		AllowAnonymous();
		Version(0);

		Summary(s => {
			s.Summary = "Get leaderboards";
		});
		
		
		Description(d => d.AutoTagOverride("Leaderboard"));
		Options(opt => opt.CacheOutput(CachePolicy.Hours));
	}

	public override async Task HandleAsync(CancellationToken c) {
		var leaderboards = lbRegistrationService.LeaderboardsById
			.ToDictionary(l => l.Key, l => new LeaderboardInfoDto {
				Title = l.Value.Info.Title,
				Short = l.Value.Info.ShortTitle,
				Category = l.Value.Info.Category,
				Profile = l.Value is IProfileLeaderboardDefinition,
				IntervalType = l.Key.EndsWith("-monthly") 
					? LeaderboardType.Monthly 
					: l.Key.EndsWith("-weekly") 
						? LeaderboardType.Weekly 
						: LeaderboardType.Current,
				ScoreDataType = l.Value.Info.ScoreDataType
			});
		
		await SendAsync(new LeaderboardsResponse() {
			Leaderboards = leaderboards,
		}, cancellation: c);
	}
}