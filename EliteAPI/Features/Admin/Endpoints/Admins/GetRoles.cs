using EliteAPI.Data;
using EliteAPI.Features.Auth.Models;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Admin.Endpoints.Admins;

internal sealed class GetRolesEndpoint(
	DataContext context)
	: EndpointWithoutRequest<List<string>> {
	public override void Configure() {
		Get("/admin/roles");
		Policies(ApiUserPolicies.Moderator);
		Version(0);

		Summary(s => { s.Summary = "Get list of roles"; });
	}

	public override async Task HandleAsync(CancellationToken c) {
		var result = await context.Roles.AsNoTracking()
			.Select(r => r.Name)
			.Where(r => r != null)
			.ToListAsync(c);

		await Send.OkAsync(result, c);
	}
}