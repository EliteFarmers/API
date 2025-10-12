using EliteAPI.Data;
using EliteAPI.Models.Entities.Events;
using EliteAPI.Parsers.Events;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Events.User.LeaveEvent;

internal sealed class LeaveEventRequest {
	[BindFrom("eventId")] public ulong EventId { get; init; }
}

internal sealed class LeaveEventEndpoint(
	DataContext context,
	UserManager userManager)
	: Endpoint<LeaveEventRequest> {
	public override void Configure() {
		Post("/event/{EventId}/leave");
		Version(0);

		Description(s => s.Accepts<LeaveEventRequest>());

		Summary(s => { s.Summary = "Join an event"; });
	}

	public override async Task HandleAsync(LeaveEventRequest request, CancellationToken c) {
		var user = await userManager.GetUserAsync(User);
		if (user?.AccountId is null) {
			await Send.UnauthorizedAsync(c);
			return;
		}

		var eliteEvent = await context.Events
			.FirstOrDefaultAsync(e => e.Id == request.EventId, c);

		if (eliteEvent is null) {
			await Send.NotFoundAsync(c);
			return;
		}

		await context.Entry(user).Reference(x => x.Account).LoadAsync(c);
		var account = user.Account;

		var members = await context.EventMembers
			.Include(e => e.ProfileMember)
			.ThenInclude(p => p.MinecraftAccount)
			.Where(e => e.EventId == request.EventId && e.ProfileMember.MinecraftAccount.AccountId == account.Id)
			.ToListAsync(c);

		if (members is not { Count: > 0 }) ThrowError("You are not a member of this event!");

		if (DateTimeOffset.UtcNow > eliteEvent.EndTime)
			ThrowError("You can no longer leave this event as it has ended.");

		foreach (var member in members) {
			member.Status = EventMemberStatus.Left;

			// Don't remove member from team in a set-team event
			if (eliteEvent.IsSetTeamEvent()) continue;

			if (member.TeamId is not null) ThrowError("You must leave your team before leaving the event.");

			member.TeamId = null;
			member.Team = null;
		}

		await context.SaveChangesAsync(c);
		await Send.NoContentAsync(c);
	}
}