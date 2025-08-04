using EliteAPI.Data;
using EliteAPI.Features.Auth.Models;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Admin.GetRoles;

internal sealed class GetRolesEndpoint(
	DataContext context)
	: EndpointWithoutRequest<string[]> 
{
	public override void Configure() {
		Get("/admin/roles");
		Policies(ApiUserPolicies.Moderator);
		Version(0);
		
		Summary(s => {
			s.Summary = "Get list of roles";
		});
	}

	public override async Task HandleAsync(CancellationToken c) {
		var result = await context.Roles.AsNoTracking()
			.Select(r => r.Name)
			.Where(r => r != null)
			.ToArrayAsync(cancellationToken: c) as string[];
		
		await Send.OkAsync(result, cancellation: c);
	}
}