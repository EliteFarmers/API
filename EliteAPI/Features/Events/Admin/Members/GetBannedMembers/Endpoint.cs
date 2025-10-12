using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Models.Common;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Events;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Events.Admin.GetBannedMembers;

internal sealed class GetBannedMembersRequest : DiscordIdRequest {
	public ulong EventId { get; set; }
}

internal sealed class GetBannedMembersAdminEndpoint(
	DataContext context,
	AutoMapper.IMapper mapper
) : Endpoint<GetBannedMembersRequest, List<AdminEventMemberDto>> {
	public override void Configure() {
		Get("/guild/{DiscordId}/event/{EventId}/bans");
		Options(o => o.WithMetadata(new GuildAdminAuthorizeAttribute()));
		Version(0);

		Summary(s => { s.Summary = "Get banned event members"; });
	}

	public override async Task HandleAsync(GetBannedMembersRequest request, CancellationToken c) {
		var @event = await context.Events
			.Where(e => e.GuildId == request.DiscordIdUlong && e.Id == request.EventId)
			.AsNoTracking()
			.FirstOrDefaultAsync(c);

		if (@event is null) {
			await Send.NotFoundAsync(c);
			return;
		}

		var members = await context.EventMembers
			.Include(m => m.ProfileMember)
			.ThenInclude(p => p.MinecraftAccount)
			.Include(m => m.ProfileMember)
			.ThenInclude(p => p.Metadata)
			.AsNoTracking()
			.Where(em => em.EventId == @event.Id &&
			             (em.Status == EventMemberStatus.Disqualified || em.Status == EventMemberStatus.Left))
			.ToListAsync(c);

		var result = mapper.Map<List<AdminEventMemberDto>>(members);
		await Send.OkAsync(result, c);
	}
}

internal sealed class CreateWeightEventRequestValidator : Validator<GetBannedMembersRequest> {
	public CreateWeightEventRequestValidator() {
		Include(new DiscordIdRequestValidator());
	}
}