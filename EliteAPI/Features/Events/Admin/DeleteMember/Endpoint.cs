using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Features.Events.Services;
using EliteAPI.Models.Common;
using EliteAPI.Utilities;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Events.Admin.DeleteMember;

internal sealed class Request : PlayerUuidRequest {
	public ulong DiscordId { get; set; }
	public ulong EventId { get; set; }
	
	[QueryParam] public string? ProfileUuid { get; set; } = null;
	[QueryParam] public int? RecordId { get; set; } = -1;
}

internal sealed class DeleteMemberEndpoint(
	IEventTeamService teamService,
	DataContext context
) : Endpoint<Request> {

	public override void Configure() {
		Delete("/guild/{DiscordId}/events/{EventId}/members/{PlayerUuid}");
		Options(o => o.WithMetadata(new GuildAdminAuthorizeAttribute()));
		Version(0);

		Summary(s => {
			s.Summary = "Delete an Event Member";
		});
	}

	public override async Task HandleAsync(Request request, CancellationToken c) {
		var userId = User.GetId();
		var @event = await context.Events
			.FirstOrDefaultAsync(e => e.Id == request.EventId && e.GuildId == request.DiscordId, cancellationToken: c);
		
		if (userId is null || @event is null) {
			await SendUnauthorizedAsync(c);
			return;
		}
		
		var member = (request.RecordId == -1) 
			? await context.EventMembers
				.Include(m => m.ProfileMember)
				.Where(em => em.EventId == @event.Id
				             && em.ProfileMember.PlayerUuid == request.PlayerUuidFormatted
				             && (request.ProfileUuid == null || em.ProfileMember.ProfileId == request.ProfileUuid))
				.FirstOrDefaultAsync(cancellationToken: c)
			: await context.EventMembers
				.Include(m => m.ProfileMember)
				.Where(em => em.EventId == @event.Id && em.Id == request.RecordId)
				.FirstOrDefaultAsync(cancellationToken: c);
        
		if (member is null) {
			await SendNotFoundAsync(c);
			return;
		}
		
		context.EventMembers.Remove(member);
		await context.SaveChangesAsync(c);

		await SendOkAsync(cancellation: c);
	}
}

internal sealed class DeleteMemberRequestValidator : Validator<Request> {
	public DeleteMemberRequestValidator() {
		Include(new PlayerUuidRequestValidator());
	}
}