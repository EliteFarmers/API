using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Models.Common;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Events;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Events.Admin.BanMember;

internal sealed class BanMemberRequest : PlayerUuidRequest {
	public ulong DiscordId { get; set; }
	public ulong EventId { get; set; }
	
	[FromBody]
	public required string Reason { get; set; }
}

internal sealed class BanMemberAdminEndpoint(
	AutoMapper.IMapper mapper,
	DataContext context
) : Endpoint<BanMemberRequest, AdminEventMemberDto> {

	public override void Configure() {
		Post("/guild/{DiscordId}/events/{EventId}/bans/{PlayerUuid}");
		Options(o => o.WithMetadata(new GuildAdminAuthorizeAttribute()));
		Version(0);

		Summary(s => {
			s.Summary = "Ban an Event Member";
		});
	}

	public override async Task HandleAsync(BanMemberRequest request, CancellationToken c) {
		var @event = await context.Events
			.FirstOrDefaultAsync(e => e.Id == request.EventId && e.GuildId == request.DiscordId, cancellationToken: c);
		
		if (@event is null) {
			await Send.NotFoundAsync(c);
			return;
		}
		
		var member = await context.EventMembers
			.Include(m => m.ProfileMember)
			.Where(em => em.EventId == @event.Id && em.ProfileMember.PlayerUuid == request.PlayerUuidFormatted)
			.FirstOrDefaultAsync(cancellationToken: c);
        
		if (member is null) {
			await Send.NotFoundAsync(c);
			return;
		}
        
		member.Status = EventMemberStatus.Disqualified;
		member.TeamId = null;
		member.Team = null;
		member.Notes = request.Reason;
        
		await context.SaveChangesAsync(c);
        
		var result = mapper.Map<AdminEventMemberDto>(member);
		await Send.OkAsync(result, cancellation: c);
	}
}

internal sealed class BanMemberRequestValidator : Validator<BanMemberRequest> {
	public BanMemberRequestValidator() {
		Include(new PlayerUuidRequestValidator());
		
		RuleFor(r => r.Reason)
			.NotEmpty()
			.WithMessage("Ban reason is required")
			.MaximumLength(128)
			.WithMessage("Ban reason must be less than or equal to 128 characters");
	}
}