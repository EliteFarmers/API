using EliteAPI.Data;
using FastEndpoints;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Graphs.GetSkillGraphs;

internal sealed class GetSkillGraphsEndpoint(
	DataContext context,
	ITimescaleService timescaleService)
	: Endpoint<GraphRequest, List<SkillsDataPointDto>> 
{
	public override void Configure() {
		Get("/graph/{PlayerUuid}/{ProfileUuid}/skills");
		AllowAnonymous();
		ResponseCache(600, varyByQueryKeys: ["start", "end", "perDay"]);
		
		Summary(s => {
			s.Summary = "Get Skill XP Over Time";
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
        
		var points = await timescaleService.GetSkills(profile, request.Start, request.End, request.PerDay ?? 4);
		await SendOkAsync(points, c);
	}
}