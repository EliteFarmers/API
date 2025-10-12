using EliteAPI.Features.Auth.Models;
using EliteAPI.Utilities;
using FastEndpoints;
using StackExchange.Redis;

namespace EliteAPI.Features.Admin.Endpoints;

internal sealed class DeleteUpcomingContestsEndpoint(
	IConnectionMultiplexer redis)
	: EndpointWithoutRequest {
	public override void Configure() {
		Delete("/admin/upcomingcontests");
		Policies(ApiUserPolicies.Moderator);
		Version(0);

		Summary(s => {
			s.Summary = "Delete all upcoming contests";
			s.Description = "Delete all upcoming contests in case of wrong data";
		});
	}

	public override async Task HandleAsync(CancellationToken c) {
		var currentYear = SkyblockDate.Now.Year;
		var db = redis.GetDatabase();

		// Delete all upcoming contests
		await db.KeyDeleteAsync($"contests:{currentYear}");
	}
}