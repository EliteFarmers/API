using EliteAPI.Configuration.Settings;
using EliteAPI.Utilities;
using FastEndpoints;
using FastEndpoints.Swagger;
using Microsoft.Extensions.Options;

namespace EliteAPI.Features.Leaderboards.Endpoints.GetLeaderboards;

internal sealed class GetLeaderboardsEndpoint(
	IOptions<ConfigLeaderboardSettings> lbSettings) 
	: EndpointWithoutRequest<ConfigLeaderboardSettings> {
	
	private readonly ConfigLeaderboardSettings _settings = lbSettings.Value;
	
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
		await SendAsync(_settings, cancellation: c);
	}
}