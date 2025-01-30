using EliteAPI.Data;
using FastEndpoints;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Graphs.GetAdminSkillGraphs;

internal sealed class GetAdminSkillGraphsEndpoint(
	DataContext context,
	ITimescaleService timescaleService)
	: Endpoint<GraphRequest, List<SkillsDataPointDto>> 
{
	public override void Configure() {
		Get("/graph/admin/{PlayerUuid}/{ProfileUuid}/skills");
		Policies(ApiUserPolicies.Support);
		
		Summary(s => {
			s.Summary = "Get Admin Skill XP";
			s.ExampleRequest = new GraphRequest {
				PlayerUuid = "7da0c47581dc42b4962118f8049147b7",
				ProfileUuid = "7da0c47581dc42b4962118f8049147b7"
			};
		});
	}

	public override async Task HandleAsync(GraphRequest request, CancellationToken c) {
		var profile = await context.ProfileMembers.AsNoTracking()
			.Where(m => m.PlayerUuid == request.PlayerUuidFormatted && m.ProfileId == request.ProfileUuidFormatted)
			.Select(p => p.Id)
			.FirstOrDefaultAsync(cancellationToken: c);

		if (profile == Guid.Empty) {
			await SendNotFoundAsync(c);
		}
        
		var points = await timescaleService.GetSkills(profile, request.Start, request.End, -1);
		await SendOkAsync(points, c);
	}
}