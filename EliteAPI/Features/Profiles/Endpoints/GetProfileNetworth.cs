using EliteAPI.Data;
using EliteAPI.Features.Profiles.Models;
using EliteAPI.Features.Profiles.Services;
using EliteAPI.Models.Common;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Profiles.Endpoints;

public class GetProfileNetworth(DataContext context, IProfileProcessorService profileProcessor)
	: Endpoint<PlayerProfileUuidRequest, NetworthBreakdown>
{
	public override void Configure() {
		Get("/profile/{PlayerUuid}/{ProfileUuid}/networth");
		AllowAnonymous();
		
		Summary(s => { s.Summary = "Get Member Networth"; });
	}

	public override async Task HandleAsync(PlayerProfileUuidRequest request, CancellationToken ct) {
		var member = await context.ProfileMembers
			.Include(p => p.Profile)
			.Include(p => p.Inventories)
			.ThenInclude(i => i.Items)
			.AsSplitQuery()
			.FirstOrDefaultAsync(
				p => p.ProfileId == request.ProfileUuidFormatted && p.PlayerUuid == request.PlayerUuidFormatted, ct);

		if (member == null) {
			await Send.NotFoundAsync(ct);
			return;
		}

		var breakdown = await profileProcessor.GetNetworthBreakdownAsync(member);

		await Send.OkAsync(breakdown, ct);
	}
}