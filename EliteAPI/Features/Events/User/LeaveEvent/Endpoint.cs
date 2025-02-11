using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Events;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Events.User.LeaveEvent;

internal sealed class LeaveEventRequest {
    public ulong EventId { get; init; }
}

internal sealed class LeaveEventEndpoint(
	DataContext context,
    UserManager userManager)
	: Endpoint<LeaveEventRequest, EventMemberDto>
{
	public override void Configure() {
		Post("/event/{EventId}/leave");
		Version(0);

		Summary(s => {
			s.Summary = "Join an event";
		});
	}

	public override async Task HandleAsync(LeaveEventRequest request, CancellationToken c) {
        var user = await userManager.GetUserAsync(User);
        if (user?.AccountId is null) {
	        await SendUnauthorizedAsync(c);
	        return;
        }
        
        var eliteEvent = await context.Events
	        .FirstOrDefaultAsync(e => e.Id == request.EventId, cancellationToken: c);
        
        if (eliteEvent is null) {
	        await SendNotFoundAsync(c);
	        return;
        }

        await context.Entry(user).Reference(x => x.Account).LoadAsync(c);
        var account = user.Account;

        var members = await context.EventMembers
            .Include(e => e.ProfileMember)
            .ThenInclude(p => p.MinecraftAccount)
            .Where(e => e.EventId == request.EventId && e.ProfileMember.MinecraftAccount.AccountId == account.Id)
            .ToListAsync(cancellationToken: c);
        
        if (members is not { Count: > 0 } ) {
            ThrowError("You are not a member of this event!");
        }
        
        if (DateTimeOffset.UtcNow > eliteEvent.EndTime) {
            ThrowError("You can no longer leave this event as it has ended.");
        }

        foreach (var member in members)
        {
            member.Status = EventMemberStatus.Left;

            if (member.TeamId is not null) {
                ThrowError("You must leave your team before leaving the event.");
            }
            
            member.TeamId = null;
            member.Team = null;
        }

        await context.SaveChangesAsync(c);
		await SendNoContentAsync(c);
	}
}
