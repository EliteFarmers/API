using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Events.Admin.GetGuildEvent;

internal sealed class GetGuildEventEndpoint(
	DataContext context,
	AutoMapper.IMapper mapper
) : Endpoint<GetAdminGuildEventRequest, EventDetailsDto> {

	public override void Configure() {
		Get("/guild/{DiscordId}/event/{EventId}/admin");
		Options(o => o.WithMetadata(new GuildAdminAuthorizeAttribute()));
		Version(0);

		Summary(s => {
			s.Summary = "Get an event for a guild";
		});
	}

	public override async Task HandleAsync(GetAdminGuildEventRequest request, CancellationToken c) {
		var @event = await context.Events
			.Where(e => e.GuildId == request.DiscordIdUlong && e.Id == request.EventId) 
			.OrderBy(e => e.StartTime)
			.AsNoTracking()
			.FirstOrDefaultAsync(cancellationToken: c);

		if (@event is null) {
			await SendNotFoundAsync(c);
			return;
		}

		var result = mapper.Map<EventDetailsDto>(@event);
		await SendAsync(result, cancellation: c);
	}
}