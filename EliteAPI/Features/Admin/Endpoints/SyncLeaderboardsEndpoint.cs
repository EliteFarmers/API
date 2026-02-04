using EliteAPI.Features.Auth.Models;
using EliteAPI.Features.Leaderboards.Services;
using FastEndpoints;

namespace EliteAPI.Features.Admin.Endpoints;

internal sealed class SyncLeaderboardsEndpoint(
	LeaderboardRedisSyncService syncService)
	: EndpointWithoutRequest
{
	public override void Configure() {
		Get("/admin/leaderboards/sync");
		Policies(ApiUserPolicies.Admin);
		Version(0);

		Summary(s => {
			s.Summary = "Sync leaderboards";
			s.Description = "Sync leaderboards in case of wrong data";
		});
	}

    public override async Task HandleAsync(CancellationToken ct)
    {
        await syncService.ForceUpdateAsync(ct);
        await Send.NoContentAsync(ct);
    }
}
