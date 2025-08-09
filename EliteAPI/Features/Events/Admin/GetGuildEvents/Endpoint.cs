using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Models.Common;
using EliteAPI.Models.DTOs.Outgoing;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Events.Admin.GetGuildEvents;

internal sealed class GetGuildEventsAdminEndpoint(
	DataContext context,
	AutoMapper.IMapper mapper
) : Endpoint<DiscordIdRequest, List<EventDetailsDto>> {
	
	public override void Configure() {
		Get("/guild/{DiscordId}/events/admin");
		Options(o => o.WithMetadata(new GuildAdminAuthorizeAttribute()));
		Version(0);

		Summary(s => {
			s.Summary = "Get all events for a guild";
		});
	}

	public override async Task HandleAsync(DiscordIdRequest request, CancellationToken c) {
		var events = await context.Events
			.Where(e => e.GuildId == request.DiscordIdUlong)
			.OrderBy(e => e.StartTime)
			.AsNoTracking()
			.ToListAsync(cancellationToken: c);

		var result = mapper.Map<List<EventDetailsDto>>(events) ?? [];
		await Send.OkAsync(result, cancellation: c);
	}
}