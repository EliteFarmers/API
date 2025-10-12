using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Features.Events.Services;
using EliteAPI.Models.Common;
using EliteAPI.Models.Entities.Events;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Events.Admin.UnbanMember;

internal sealed class UnbanMemberRequest : PlayerUuidRequest {
	public ulong DiscordId { get; set; }
	public ulong EventId { get; set; }
}

internal sealed class UnbanMemberAdminEndpoint(
	DataContext context
) : Endpoint<UnbanMemberRequest> {
	public override void Configure() {
		Delete("/guild/{DiscordId}/events/{EventId}/bans/{PlayerUuid}");
		Options(o => o.WithMetadata(new GuildAdminAuthorizeAttribute()));
		Version(0);

		Summary(s => { s.Summary = "Unban an Event Member"; });
	}

	public override async Task HandleAsync(UnbanMemberRequest request, CancellationToken c) {
		var @event = await context.Events
			.FirstOrDefaultAsync(e => e.Id == request.EventId && e.GuildId == request.DiscordId, c);

		if (@event is null) {
			await Send.NotFoundAsync(c);
			return;
		}

		var member = await context.EventMembers
			.Include(m => m.ProfileMember)
			.Where(em => em.EventId == @event.Id && em.ProfileMember.PlayerUuid == request.PlayerUuidFormatted)
			.FirstOrDefaultAsync(c);

		if (member is null) {
			await Send.NotFoundAsync(c);
			return;
		}

		member.Status = EventMemberStatus.Active;
		member.TeamId = null;
		member.Team = null;
		await context.SaveChangesAsync(c);

		await Send.NoContentAsync(c);
	}
}

internal sealed class UnbanMemberRequestValidator : Validator<UnbanMemberRequest> {
	public UnbanMemberRequestValidator() {
		Include(new PlayerUuidRequestValidator());
	}
}