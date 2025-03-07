using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Features.Events.Services;
using EliteAPI.Models.Common;
using EliteAPI.Models.DTOs.Outgoing;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Events.Admin.ForceAddMember;

internal sealed class ForceAddMemberRequest : PlayerUuidRequest {
	public ulong DiscordId { get; set; }
	public ulong EventId { get; set; }
	
	[QueryParam]
	public required string ProfileUuid { get; set; }
}

internal sealed class ForceAddMemberEndpoint(
	DataContext context,
	IEventService eventService
) : Endpoint<ForceAddMemberRequest> {

	public override void Configure() {
		Post("/guild/{DiscordId}/events/{EventId}/members/{PlayerUuid}");
		Options(o => o.WithMetadata(new GuildAdminAuthorizeAttribute()));
		Version(0);
		
		Description(x => x.Accepts<ForceAddMemberRequest>());

		Summary(s => {
			s.Summary = "Ban an Event Member";
		});
	}

	public override async Task HandleAsync(ForceAddMemberRequest request, CancellationToken c) {
		var @event = await context.Events
			.FirstOrDefaultAsync(e => e.Id == request.EventId && e.GuildId == request.DiscordId, cancellationToken: c);
		
		if (@event is null) {
			await SendNotFoundAsync(c);
			return;
		}
		
		var member = await context.ProfileMembers.AsNoTracking()
			.Include(p => p.MinecraftAccount)
			.Where(p => p.PlayerUuid == request.PlayerUuidFormatted && p.ProfileId == request.ProfileUuid)
			.FirstOrDefaultAsync(cancellationToken: c);

		if (member?.MinecraftAccount.AccountId is null) {
			ThrowError("Player not found, or player does not have a linked account.");
		}

		var existing = await context.EventMembers.AsNoTracking()
			.Where(em => em.EventId == @event.Id && em.ProfileMemberId == member.Id)
			.FirstOrDefaultAsync(cancellationToken: c);
        
		if (existing is not null) {
			ThrowError("Player is already in the event.");
		}
        
		await eventService.CreateEventMember(@event, new CreateEventMemberDto {
			EventId = @event.Id,
			ProfileMemberId = member.Id,
			UserId = member.MinecraftAccount.AccountId.Value,
			Score = 0,
			StartTime = @event.StartTime,
			EndTime = @event.EndTime,
			ProfileMember = member
		});
        
		await context.SaveChangesAsync(c);
		await SendNoContentAsync(cancellation: c);
	}
}

internal sealed class ForceAddMemberRequestValidator : Validator<ForceAddMemberRequest> {
	public ForceAddMemberRequestValidator() {
		Include(new PlayerUuidRequestValidator());
	}
}