using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Models.Common;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Events;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Events.Admin.GetEventMembers;

internal sealed class Request : DiscordIdRequest {
	public ulong EventId { get; set; }
}

internal sealed class GetEventMembersEndpoint(
	DataContext context,
	AutoMapper.IMapper mapper
) : Endpoint<Request, List<AdminEventMemberDto>> {

	public override void Configure() {
		Get("/guild/{DiscordId}/event/{EventId}/members");
		Options(o => o.WithMetadata(new GuildAdminAuthorizeAttribute()));
		Version(0);

		Summary(s => {
			s.Summary = "Get event members";
		});
	}

	public override async Task HandleAsync(Request request, CancellationToken c) {
		var @event = await context.Events
			.Where(e => e.GuildId == request.DiscordIdUlong && e.Id == request.EventId) 
			.AsNoTracking()
			.FirstOrDefaultAsync(cancellationToken: c);

		if (@event is null) {
			await SendNotFoundAsync(c);
			return;
		}
		
		var members = await context.EventMembers
			.Include(m => m.ProfileMember)
			.ThenInclude(p => p.MinecraftAccount).AsNoTracking()
			.Where(em => em.EventId == @event.Id &&
			             (em.Status == EventMemberStatus.Active || em.Status == EventMemberStatus.Inactive))
			.ToListAsync(cancellationToken: c);
        
		var result = mapper.Map<List<AdminEventMemberDto>>(members);
		await SendAsync(result, cancellation: c);
	}
}

internal sealed class CreateWeightEventRequestValidator : Validator<Request> {
	public CreateWeightEventRequestValidator() {
		Include(new DiscordIdRequestValidator());
	}
}