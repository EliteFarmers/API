using EliteAPI.Data;
using FastEndpoints;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Graphs.GetCropGraphs;

internal sealed class GetCropGraphsEndpoint(
	DataContext context,
	ITimescaleService timescaleService)
	: Endpoint<GraphRequest, List<CropCollectionsDataPointDto>> 
{
	public override void Configure() {
		Get("/graph/{PlayerUuid}/{ProfileUuid}/crops");
		AllowAnonymous();
		
		Summary(s => {
			s.Summary = "Get Crop Collections Over Time";
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
        
		var points = await timescaleService.GetCropCollections(profile, request.Start, request.End, request.PerDay ?? 4);
		await SendOkAsync(points, c);
	}
}