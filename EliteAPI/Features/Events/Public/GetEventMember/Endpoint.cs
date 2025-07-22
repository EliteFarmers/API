using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Features.Auth.Models;
using EliteAPI.Models.Common;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Events;
using EliteAPI.Utilities;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Events.Public.GetEventMember;

internal sealed class GetEventMemberRequest : PlayerUuidRequest {
	public ulong EventId { get; set; }
}
internal sealed class GetEventMembersEndpoint(
	DataContext context,
	AutoMapper.IMapper mapper)
	: Endpoint<GetEventMemberRequest, EventMemberDto>
{
	public override void Configure() {
		Get("/event/{EventId}/member/{PlayerUuid}");
		Options(o => o.WithMetadata(new OptionalAuthorizeAttribute()));
		AllowAnonymous();
		Version(0);

		Summary(s => {
			s.Summary = "Get an event member";
		});
		
		Options(opt => opt.CacheOutput(o => o.Expire(TimeSpan.FromMinutes(2))));
	}

	public override async Task HandleAsync(GetEventMemberRequest request, CancellationToken c) {
		var member = await context.EventMembers.AsNoTracking()
			.Include(e => e.ProfileMember)
			.ThenInclude(p => p.MinecraftAccount)
			.AsNoTracking()
			.FirstOrDefaultAsync(e => e.EventId == request.EventId && e.ProfileMember.PlayerUuid == request.PlayerUuid, cancellationToken: c);
		
		if (member is null) {
			await SendNotFoundAsync(c);
			return;
		}
        
		var mapped = mapper.Map<EventMemberDto>(member);

		mapped.Data = member switch {
			WeightEventMember m => m.Data,
			MedalEventMember m => m.Data,
			CollectionEventMember m => m.Data,
			PestEventMember m => m.Data,
			_ => mapped.Data
		};
		
		// If the user is the member or a moderator, send the notes
		if (User.GetId() is { } id && (id == member.UserId.ToString() || User.IsInRole(ApiUserPolicies.Moderator))) {
			await SendAsync(mapped, cancellation: c);
			return;
		}

		mapped.Notes = null;
		await SendAsync(mapped, cancellation: c);
	}
}

internal sealed class GetEventMemberRequestValidator : Validator<GetEventMemberRequest> {
	public GetEventMemberRequestValidator() {
		Include(new PlayerUuidRequestValidator());
	}
}
