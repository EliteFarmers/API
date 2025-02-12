using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Events;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Events.Public.GetEventMembers;

internal sealed class GetEventMembersRequest {
	public ulong EventId { get; set; }
}

internal sealed class GetEventMembersEndpoint(
	DataContext context,
	AutoMapper.IMapper mapper)
	: Endpoint<GetEventMembersRequest, List<EventMemberDetailsDto>>
{
	public override void Configure() {
		Get("/event/{EventId}/members");
		AllowAnonymous();
		Version(0);

		Summary(s => {
			s.Summary = "Get event members";
		});
		
		Options(opt => opt.CacheOutput(o => o.Expire(TimeSpan.FromMinutes(2))));
	}

	public override async Task HandleAsync(GetEventMembersRequest request, CancellationToken c) {
		var eliteEvent = await context.Events.AsNoTracking()
			.FirstOrDefaultAsync(e => e.Id == request.EventId && e.Approved, cancellationToken: c);
		
		if (eliteEvent is null) {
			await SendNotFoundAsync(c);
			return;
		}
        
		var isTeamEvent = eliteEvent.GetMode() != EventTeamMode.Solo;

		var members = await context.EventMembers.AsNoTracking()
			.Include(e => e.ProfileMember)
			.ThenInclude(p => p.MinecraftAccount)
			.AsNoTracking()
			.Where(e => e.EventId == request.EventId 
			            && e.Status != EventMemberStatus.Disqualified 
			            && e.Status != EventMemberStatus.Left
			            && (e.TeamId != null || !isTeamEvent))
			.OrderByDescending(e => e.Score)
			.AsSplitQuery()
			.ToListAsync(cancellationToken: c);

		if (eliteEvent.Type == EventType.Medals) {
			members = members.OrderByDescending(e => e as MedalEventMember).ToList();
		}

		var result = members.Select(mapper.Map<EventMemberDetailsDto>).ToList();

		await SendAsync(result, cancellation: c);
	}
}